using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Recam.Server.Domain;

namespace Recam.Server.Infrastructure.Auth;

public static class AuthExtensions
{
    public const string OwnerOnly = nameof(OwnerOnly);
    public const string ViewerOrOwner = nameof(ViewerOrOwner);
    public const string CameraOnly = nameof(CameraOnly);

    public static IServiceCollection AddRecamAuth(this IServiceCollection services)
    {
        services.AddAuthentication(DeviceAuthenticationHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, DeviceAuthenticationHandler>(DeviceAuthenticationHandler.SchemeName, null);

        services.AddAuthorizationBuilder()
            .AddPolicy(OwnerOnly, policy => policy.RequireRole(nameof(DeviceRole.Owner)))
            .AddPolicy(ViewerOrOwner, policy => policy.RequireRole(nameof(DeviceRole.Owner), nameof(DeviceRole.Viewer)))
            .AddPolicy(CameraOnly, policy => policy.RequireRole(nameof(DeviceRole.Camera)));
        return services;
    }

    public static Guid GetDeviceId(this ClaimsPrincipal user) =>
        Guid.ParseExact(user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Authenticated device without an id claim."), "N");
}
