using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Recam.Server.Domain;
using Recam.Server.Infrastructure.Auth;
using Recam.Server.Infrastructure.Hosting;
using Recam.Server.Infrastructure.Http;
using Recam.Server.Infrastructure.Network;
using Recam.Server.Infrastructure.Persistence;
using Recam.Server.Infrastructure.Tls;

namespace Recam.Server.Features.Pairing;

public static partial class PairingEndpoints
{
    public const string ServerName = "Recam";

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
            .RequireAuthorization(AuthExtensions.OwnerOnly);
        return endpoints;
    }

    private static async Task<IResult> PairAsync(
        PairRequest request,
        IDbContextFactory<RecamDbContext> databaseFactory,
        TimeProvider timeProvider,
        ILoggerFactory loggerFactory,
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

        var paired = Device.Pair(token, validation.Value.Name, timeProvider.GetUtcNow());
        if (paired.IsFailure)
        {
            LogRejected(logger, paired.Error.Code);
            return PairingErrors.InvalidToken.ToHttpResult();
        }

        var device = paired.Value.Device;
        database.Devices.Add(device);
        await database.SaveChangesAsync(cancellationToken);
        LogPaired(logger, device.Id, device.Role);

        return TypedResults.Created(
            "/api/me",
            new PairResponse(device.Id, paired.Value.Credential, device.Role, ServerName));
    }

    private static async Task<IResult> CreatePairingTokenAsync(
        CreatePairingTokenRequest request,
        ClaimsPrincipal user,
        IDbContextFactory<RecamDbContext> databaseFactory,
        TimeProvider timeProvider,
        ServerCertificate certificate,
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
        var ownerId = user.GetDeviceId();
        var owner = await database.Devices.SingleAsync(device => device.Id == ownerId, cancellationToken);
        var grantedRole = validation.Value;
        var issued = owner.IssuePairingToken(grantedRole, timeProvider.GetUtcNow());
        if (issued.IsFailure)
        {
            return issued.Error.ToHttpResult();
        }

        database.PairingTokens.Add(issued.Value.Token);
        await database.SaveChangesAsync(cancellationToken);
        var logger = loggerFactory.CreateLogger(LogCategory);
        LogTokenIssued(logger, owner.Id, grantedRole);

        var serverUrls = PublicUrlResolver.Resolve(settings, PublicUrlResolver.DetectLocalAddresses());
        return TypedResults.Created(
            "/api/pairing-tokens",
            new CreatePairingTokenResponse(
                PairingUri.Build(issued.Value.Secret, certificate.Fingerprint, serverUrls),
                issued.Value.Token.ExpiresAt));
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Pairing rejected: {Reason}")]
    private static partial void LogRejected(ILogger logger, string reason);

    [LoggerMessage(Level = LogLevel.Information, Message = "Device {DeviceId} paired as {Role}")]
    private static partial void LogPaired(ILogger logger, Guid deviceId, DeviceRole role);

    [LoggerMessage(Level = LogLevel.Information, Message = "Device {DeviceId} created a {Role} pairing token")]
    private static partial void LogTokenIssued(ILogger logger, Guid deviceId, DeviceRole role);
}
