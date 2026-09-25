using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Recam.Server.Domain;
using Recam.Server.Infrastructure.Persistence;

namespace Recam.Server.Infrastructure.Auth;

/// <summary>
/// Authenticates "Authorization: Bearer &lt;device credential&gt;" (SPECS.md 5.4). Hub
/// connections may send it as the access_token query value instead.
/// </summary>
public sealed class DeviceAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IDbContextFactory<RecamDbContext> databaseFactory)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Device";
    private const string BearerPrefix = "Bearer ";
    private const string HubPathPrefix = "/hubs";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var credential = ReadCredential();
        if (credential is null)
        {
            return AuthenticateResult.NoResult();
        }

        if (!DeviceCredential.TryParse(credential, out var deviceId, out var secret))
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

    // WebSockets opened by browsers and the SignalR clients cannot set headers, so the hub
    // also accepts the credential in the query string. Nowhere else, to keep it out of URLs.
    private string? ReadCredential()
    {
        var header = Request.Headers.Authorization.ToString();
        if (header.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return header[BearerPrefix.Length..].Trim();
        }

        if (Request.Path.StartsWithSegments(HubPathPrefix, StringComparison.OrdinalIgnoreCase)
            && Request.Query["access_token"].ToString() is { Length: > 0 } token)
        {
            return token;
        }

        return null;
    }
}
