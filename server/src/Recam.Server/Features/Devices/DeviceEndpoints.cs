using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Recam.Server.Domain;
using Recam.Server.Infrastructure.Auth;

namespace Recam.Server.Features.Devices;

public static class DeviceEndpoints
{
    public static IEndpointRouteBuilder MapDeviceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/me", GetMe).RequireAuthorization();
        return endpoints;
    }

    private static Ok<MeResponse> GetMe(ClaimsPrincipal user) =>
        TypedResults.Ok(new MeResponse(
            user.GetDeviceId(),
            user.FindFirstValue(ClaimTypes.Name) ?? string.Empty,
            Enum.Parse<DeviceRole>(user.FindFirstValue(ClaimTypes.Role) ?? string.Empty)));
}
