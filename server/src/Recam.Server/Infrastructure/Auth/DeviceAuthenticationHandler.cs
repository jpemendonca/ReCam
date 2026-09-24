using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Recam.Server.Domain;
using Recam.Server.Infrastructure.Persistence;

namespace Recam.Server.Infrastructure.Auth;

/// <summary>Authenticates "Authorization: Bearer &lt;device credential&gt;" (SPECS.md 5.4).</summary>
public sealed class DeviceAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IDbContextFactory<RecamDbContext> databaseFactory)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Device";
    private const string BearerPrefix = "Bearer ";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var header = Request.Headers.Authorization.ToString();
        if (!header.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.NoResult();
        }

        if (!DeviceCredential.TryParse(header[BearerPrefix.Length..].Trim(), out var deviceId, out var secret))
        {
            return AuthenticateResult.Fail("Malformed device credential.");
        }

        await using var database = await databaseFactory.CreateDbContextAsync(Context.RequestAborted);
        var device = await database.Devices.AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == deviceId, Context.RequestAborted);
        if (device is null || device.IsRevoked || !device.HasCredentialSecret(secret))
        {
            return AuthenticateResult.Fail("Unknown, revoked or wrong device credential.");
        }

        Claim[] claims =
        [
            new(ClaimTypes.NameIdentifier, device.Id.ToString("N")),
            new(ClaimTypes.Name, device.Name),
            new(ClaimTypes.Role, device.Role.ToString()),
        ];
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, SchemeName));
        return AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName));
    }
}
