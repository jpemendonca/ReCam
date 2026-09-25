using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Recam.Server.Domain;
using Recam.Server.Infrastructure.Auth;
using Recam.Server.Infrastructure.Persistence;
using Recam.Server.Infrastructure.Presence;

namespace Recam.Server.Features.Devices;

public static class DeviceEndpoints
{
    public static IEndpointRouteBuilder MapDeviceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/me", GetMe).RequireAuthorization();
        endpoints.MapGet("/api/cameras", GetCamerasAsync).RequireAuthorization(AuthExtensions.ViewerOrOwner);
        return endpoints;
    }

    private static Ok<MeResponse> GetMe(ClaimsPrincipal user) =>
        TypedResults.Ok(new MeResponse(
            user.GetDeviceId(),
            user.FindFirstValue(ClaimTypes.Name) ?? string.Empty,
            Enum.Parse<DeviceRole>(user.FindFirstValue(ClaimTypes.Role) ?? string.Empty)));

    private static async Task<Ok<List<CameraStatus>>> GetCamerasAsync(
        IDbContextFactory<RecamDbContext> databaseFactory, DevicePresence presence, CancellationToken cancellationToken)
    {
        await using var database = await databaseFactory.CreateDbContextAsync(cancellationToken);
        var cameras = await database.Devices.AsNoTracking()
            .Where(device => device.Role == DeviceRole.Camera && device.RevokedAt == null)
            .OrderBy(device => device.Name)
            .ToListAsync(cancellationToken);
        return TypedResults.Ok(cameras
            .Select(camera => camera.ToCameraStatus(presence.IsOnline(camera.Id), publishing: false))
            .ToList());
    }
}
