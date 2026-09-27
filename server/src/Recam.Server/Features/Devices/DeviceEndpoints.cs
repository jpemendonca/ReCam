using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Recam.Server.Domain;
using Recam.Server.Infrastructure.Auth;
using Recam.Server.Infrastructure.Http;
using Recam.Server.Infrastructure.Persistence;
using Recam.Server.Infrastructure.Presence;
using Recam.Server.Infrastructure.Realtime;
using Recam.Server.Infrastructure.Recordings;

namespace Recam.Server.Features.Devices;

public static class DeviceEndpoints
{
    public static IEndpointRouteBuilder MapDeviceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/me", GetMe).RequireAuthorization();
        endpoints.MapDelete("/api/me", LeaveAsync).RequireAuthorization();
        endpoints.MapGet("/api/cameras", GetCamerasAsync).RequireAuthorization(AuthExtensions.ViewerOrOwner);
        endpoints.MapGet("/api/devices", GetDevicesAsync).RequireAuthorization(AuthExtensions.ViewerOrOwner);
        endpoints.MapDelete("/api/devices/{id:guid}", RemoveDeviceAsync).RequireAuthorization(AuthExtensions.ViewerOrOwner);
        return endpoints;
    }

    private static Ok<MeResponse> GetMe(ClaimsPrincipal user) =>
        TypedResults.Ok(new MeResponse(
            user.GetDeviceId(),
            user.FindFirstValue(ClaimTypes.Name) ?? string.Empty,
            Enum.Parse<DeviceRole>(user.FindFirstValue(ClaimTypes.Role) ?? string.Empty)));

    /// <summary>
    /// A phone that resets the app takes itself off the server, so it no longer counts as a
    /// camera or a Monitor.
    /// </summary>
    private static async Task<NoContent> LeaveAsync(
        ClaimsPrincipal user,
        IDbContextFactory<RecamDbContext> databaseFactory,
        TimeProvider timeProvider,
        IDeviceRemovals removals,
        CancellationToken cancellationToken)
    {
        await using var database = await databaseFactory.CreateDbContextAsync(cancellationToken);
        var deviceId = user.GetDeviceId();
        var device = await database.Devices.SingleAsync(candidate => candidate.Id == deviceId, cancellationToken);
        device.Revoke(timeProvider.GetUtcNow());
        await database.SaveChangesAsync(cancellationToken);
        await removals.DeviceRemovedAsync(device.Id, device.Role == DeviceRole.Camera);
        return TypedResults.NoContent();
    }

    /// <summary>Every device still on the server, cameras first, for the Monitor's device list.</summary>
    private static async Task<Ok<List<DeviceResponse>>> GetDevicesAsync(
        IDbContextFactory<RecamDbContext> databaseFactory, DevicePresence presence, CancellationToken cancellationToken)
    {
        await using var database = await databaseFactory.CreateDbContextAsync(cancellationToken);
        var devices = await database.Devices.AsNoTracking()
            .Where(device => device.RevokedAt == null)
            .ToListAsync(cancellationToken);
        return TypedResults.Ok(devices
            .OrderBy(device => device.Role == DeviceRole.Camera ? 0 : 1)
            .ThenBy(device => device.Name, StringComparer.CurrentCultureIgnoreCase)
            .Select(device => new DeviceResponse(device.Id, device.Name, device.Role, presence.IsOnline(device.Id)))
            .ToList());
    }

    /// <summary>A Monitor removes another device: revoked, cut off, and gone from the lists.</summary>
    private static async Task<IResult> RemoveDeviceAsync(
        Guid id,
        ClaimsPrincipal user,
        IDbContextFactory<RecamDbContext> databaseFactory,
        TimeProvider timeProvider,
        IDeviceRemovals removals,
        CancellationToken cancellationToken)
    {
        await using var database = await databaseFactory.CreateDbContextAsync(cancellationToken);
        var requesterId = user.GetDeviceId();
        var requester = await database.Devices.SingleAsync(device => device.Id == requesterId, cancellationToken);
        var target = await database.Devices.SingleOrDefaultAsync(device => device.Id == id, cancellationToken);
        if (target is null)
        {
            return DeviceErrors.NotFound.ToHttpResult();
        }

        var removed = target.RevokeBy(requester, timeProvider.GetUtcNow());
        if (removed.IsFailure)
        {
            return removed.Error.ToHttpResult();
        }

        await database.SaveChangesAsync(cancellationToken);
        await removals.DeviceRemovedAsync(target.Id, target.Role == DeviceRole.Camera);
        return TypedResults.NoContent();
    }

    private static async Task<Ok<List<CameraStatus>>> GetCamerasAsync(
        IDbContextFactory<RecamDbContext> databaseFactory, RecordingStateTracker recordingStates, CancellationToken cancellationToken)
    {
        await using var database = await databaseFactory.CreateDbContextAsync(cancellationToken);
        var cameras = await database.Devices.AsNoTracking()
            .Where(device => device.Role == DeviceRole.Camera && device.RevokedAt == null)
            .OrderBy(device => device.Name)
            .ToListAsync(cancellationToken);
        return TypedResults.Ok(cameras
            .Select(recordingStates.StatusOf)
            .ToList());
    }
}
