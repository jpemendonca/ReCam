using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Recam.Server.Domain;
using Recam.Server.Infrastructure.Auth;
using Recam.Server.Infrastructure.Hosting;
using Recam.Server.Infrastructure.Http;
using Recam.Server.Infrastructure.Network;
using Recam.Server.Infrastructure.Persistence;
using Recam.Server.Infrastructure.Realtime;
using Recam.Server.Infrastructure.Tls;

namespace Recam.Server.Features.Pairing;

public static partial class PairingEndpoints
{
    public const string ServerName = "ReCam";

    private const string RateLimitPolicy = "pairing";
    private const string LogCategory = "Recam.Server.Features.Pairing";

    public static IServiceCollection AddPairing(this IServiceCollection services)
    {
        services.AddRateLimiter(options => options.AddPolicy(RateLimitPolicy, context =>
            RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 })));
        return services;
    }

    public static IEndpointRouteBuilder MapPairingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/pair", PairAsync)
            .AllowAnonymous()
            .RequireRateLimiting(RateLimitPolicy);
        endpoints.MapPost("/api/pairing-tokens", CreatePairingTokenAsync)
            .RequireAuthorization(AuthExtensions.ViewerOrOwner);
        endpoints.MapGet("/api/pairing-tokens/{id:guid}", GetPairingTokenAsync)
            .RequireAuthorization(AuthExtensions.ViewerOrOwner);
        return endpoints;
    }

    private static async Task<IResult> PairAsync(
        PairRequest request,
        IDbContextFactory<RecamDbContext> databaseFactory,
        TimeProvider timeProvider,
        ILoggerFactory loggerFactory,
        IDeviceListChanges listChanges,
        CancellationToken cancellationToken)
    {
        var validation = PairRequestValidator.Validate(request);
        if (validation.IsFailure)
        {
            return validation.Error.ToHttpResult();
        }

        var logger = loggerFactory.CreateLogger(LogCategory);
        await using var database = await databaseFactory.CreateDbContextAsync(cancellationToken);
        var tokenHash = SecretToken.Hash(validation.Value.Token);
        var token = await database.PairingTokens.SingleOrDefaultAsync(candidate => candidate.TokenHash == tokenHash, cancellationToken);
        if (token is null)
        {
            LogRejected(logger, "pairing.unknown_token");
            return PairingErrors.InvalidToken.ToHttpResult();
        }

        var paired = Device.Pair(token, validation.Value.Name, validation.Value.ExpectedRoles, timeProvider.GetUtcNow());
        if (paired.IsFailure)
        {
            LogRejected(logger, paired.Error.Code);
            return paired.Error == PairingErrors.WrongRole
                ? paired.Error.ToHttpResult()
                : PairingErrors.InvalidToken.ToHttpResult();
        }

        var device = paired.Value.Device;
        database.Devices.Add(device);
        await database.SaveChangesAsync(cancellationToken);
        LogPaired(logger, device.Id, device.Role);
        await listChanges.DevicesChangedAsync();

        return TypedResults.Created(
            "/api/me",
            new PairResponse(device.Id, paired.Value.Credential, device.Role, ServerName));
    }

    private static async Task<IResult> CreatePairingTokenAsync(
        CreatePairingTokenRequest request,
        ClaimsPrincipal user,
        IDbContextFactory<RecamDbContext> databaseFactory,
        TimeProvider timeProvider,
        CertificatePin certificate,
        ServerSettings settings,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var validation = CreatePairingTokenRequestValidator.Validate(request);
        if (validation.IsFailure)
        {
            return validation.Error.ToHttpResult();
        }

        await using var database = await databaseFactory.CreateDbContextAsync(cancellationToken);
        var issuerId = user.GetDeviceId();
        var issuer = await database.Devices.SingleAsync(device => device.Id == issuerId, cancellationToken);
        var grantedRole = validation.Value;
        var issued = issuer.IssuePairingToken(grantedRole, timeProvider.GetUtcNow());
        if (issued.IsFailure)
        {
            return issued.Error.ToHttpResult();
        }

        database.PairingTokens.Add(issued.Value.Token);
        await database.SaveChangesAsync(cancellationToken);
        var logger = loggerFactory.CreateLogger(LogCategory);
        LogTokenIssued(logger, issuer.Id, grantedRole);

        var serverUrls = PublicUrlResolver.Resolve(settings, PublicUrlResolver.DetectLocalAddresses());
        return TypedResults.Created(
            "/api/pairing-tokens",
            new CreatePairingTokenResponse(
                issued.Value.Token.Id,
                PairingUri.Build(issued.Value.Secret, grantedRole, certificate.Fingerprint, serverUrls),
                issued.Value.Token.ExpiresAt));
    }

    /// <summary>Lets the phone showing a QR code close it once another phone has paired.</summary>
    private static async Task<IResult> GetPairingTokenAsync(
        Guid id,
        ClaimsPrincipal user,
        IDbContextFactory<RecamDbContext> databaseFactory,
        CancellationToken cancellationToken)
    {
        await using var database = await databaseFactory.CreateDbContextAsync(cancellationToken);
        var token = await database.PairingTokens.AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (token is null)
        {
            return PairingErrors.TokenNotFound.ToHttpResult();
        }

        var usage = token.UsageFor(user.GetDeviceId());
        return usage.IsFailure
            ? usage.Error.ToHttpResult()
            : TypedResults.Ok(new PairingTokenStatusResponse(usage.Value));
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Pairing rejected: {Reason}")]
    private static partial void LogRejected(ILogger logger, string reason);

    [LoggerMessage(Level = LogLevel.Information, Message = "Device {DeviceId} paired as {Role}")]
    private static partial void LogPaired(ILogger logger, Guid deviceId, DeviceRole role);

    [LoggerMessage(Level = LogLevel.Information, Message = "Device {DeviceId} created a {Role} pairing token")]
    private static partial void LogTokenIssued(ILogger logger, Guid deviceId, DeviceRole role);
}
