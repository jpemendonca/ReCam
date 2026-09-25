using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Recam.Server.Domain;
using Recam.Server.Infrastructure.Auth;
using Recam.Server.Infrastructure.Persistence;
using Recam.Server.Infrastructure.Presence;
using Recam.Server.Infrastructure.Realtime;

namespace Recam.Server.Features.Realtime;

/// <summary>
/// One long-lived connection per app tab. Tracks presence and relays camera state to viewers.
/// Method names are the wire protocol, so they carry no Async suffix.
/// </summary>
[Authorize]
public sealed partial class DeviceHub(
    IDbContextFactory<RecamDbContext> databaseFactory,
    DevicePresence presence,
    WatchLeases leases,
    TimeProvider timeProvider,
    ILogger<DeviceHub> logger) : Hub<IDeviceClient>
{
    public const string Path = "/hubs/devices";
    public const string ViewersGroup = "viewers";

    public override async Task OnConnectedAsync()
    {
        var user = Context.User!;
        var deviceId = user.GetDeviceId();
        var role = user.FindFirstValue(ClaimTypes.Role);
        LogConnected(logger, deviceId, role, Context.ConnectionId);
        if (user.IsInRole(nameof(DeviceRole.Owner)) || user.IsInRole(nameof(DeviceRole.Viewer)))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, ViewersGroup, Context.ConnectionAborted);
        }

        if (presence.Connect(deviceId))
        {
            await PresenceChangedAsync(deviceId, Context.ConnectionAborted);
        }

        // A camera that reconnects while someone watches resumes publishing on its own.
        if (user.IsInRole(nameof(DeviceRole.Camera)))
        {
            var watchers = presence.Watchers(deviceId);
            if (watchers > 0)
            {
                await Clients.Caller.StartPublishing();
            }

            await Clients.Caller.WatchersChanged(watchers);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await leases.RemoveConnectionAsync(Context.ConnectionId);
        var deviceId = Context.User!.GetDeviceId();
        var reason = exception?.Message;
        LogDisconnected(logger, deviceId, Context.ConnectionId, reason);
        if (presence.Disconnect(deviceId))
        {
            await PresenceChangedAsync(deviceId, CancellationToken.None);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Lets a client prove its connection still works end to end. Some clients do not notice a
    /// dropped socket on their own.
    /// </summary>
    public HubResult Heartbeat() => HubResult.Success;

    [Authorize(Policy = AuthExtensions.CameraOnly)]
    public async Task<HubResult> ReportTelemetry(int batteryLevel, bool isCharging)
    {
        await using var database = await databaseFactory.CreateDbContextAsync(Context.ConnectionAborted);
        var camera = await database.Devices.SingleAsync(
            device => device.Id == Context.User!.GetDeviceId(), Context.ConnectionAborted);
        var reported = camera.ReportTelemetry(batteryLevel, isCharging, timeProvider.GetUtcNow());
        if (reported.IsFailure)
        {
            return reported.Error.ToHubResult();
        }

        await database.SaveChangesAsync(Context.ConnectionAborted);
        await NotifyViewersAsync(camera);
        return HubResult.Success;
    }

    [Authorize(Policy = AuthExtensions.ViewerOrOwner)]
    public async Task<HubResult> WatchCamera(Guid cameraId)
    {
        await using var database = await databaseFactory.CreateDbContextAsync(Context.ConnectionAborted);
        var exists = await database.Devices.AnyAsync(
            device => device.Id == cameraId && device.Role == DeviceRole.Camera && device.RevokedAt == null,
            Context.ConnectionAborted);
        if (!exists)
        {
            return MediaErrors.CameraNotFound.ToHubResult();
        }

        await leases.WatchAsync(Context.ConnectionId, cameraId);
        return HubResult.Success;
    }

    [Authorize(Policy = AuthExtensions.ViewerOrOwner)]
    public async Task<HubResult> UnwatchCamera(Guid cameraId)
    {
        await leases.UnwatchAsync(Context.ConnectionId, cameraId);
        return HubResult.Success;
    }

    [Authorize(Policy = AuthExtensions.CameraOnly)]
    public async Task<HubResult> ReportPublishing(bool publishing)
    {
        var cameraId = Context.User!.GetDeviceId();
        presence.SetPublishing(cameraId, publishing);
        await using var database = await databaseFactory.CreateDbContextAsync(Context.ConnectionAborted);
        var camera = await database.Devices.SingleAsync(device => device.Id == cameraId, Context.ConnectionAborted);
        await NotifyViewersAsync(camera);
        return HubResult.Success;
    }

    [Authorize(Policy = AuthExtensions.ViewerOrOwner)]
    public async Task<HubResult> SetTorch(Guid cameraId, bool torchOn)
    {
        await using var database = await databaseFactory.CreateDbContextAsync(Context.ConnectionAborted);
        var camera = await database.Devices.AsNoTracking()
            .SingleOrDefaultAsync(device => device.Id == cameraId, Context.ConnectionAborted);
        if (camera is null)
        {
            return MediaErrors.CameraNotFound.ToHubResult();
        }

        var accepted = camera.AcceptTorchCommand(presence.IsPublishing(cameraId));
        if (accepted.IsFailure)
        {
            return accepted.Error.ToHubResult();
        }

        await Clients.User(UserId(cameraId)).SetTorch(torchOn);
        return HubResult.Success;
    }

    /// <summary>The camera tells what its torch really did; viewers show that, not what they asked.</summary>
    [Authorize(Policy = AuthExtensions.CameraOnly)]
    public async Task<HubResult> ReportTorch(bool torchOn)
    {
        await Clients.Group(ViewersGroup).TorchChanged(Context.User!.GetDeviceId(), torchOn);
        return HubResult.Success;
    }

    /// <summary>The SignalR user id of a device: the id claim, in N format.</summary>
    internal static string UserId(Guid deviceId) => deviceId.ToString("N");

    private async Task PresenceChangedAsync(Guid deviceId, CancellationToken cancellationToken)
    {
        await using var database = await databaseFactory.CreateDbContextAsync(cancellationToken);
        var device = await database.Devices.SingleAsync(candidate => candidate.Id == deviceId, cancellationToken);
        device.MarkSeen(timeProvider.GetUtcNow());
        await database.SaveChangesAsync(cancellationToken);
        if (device.Role == DeviceRole.Camera)
        {
            await NotifyViewersAsync(device);
        }
    }

    private Task NotifyViewersAsync(Device camera) =>
        Clients.Group(ViewersGroup).CameraStatusChanged(camera.ToCameraStatus(presence.IsOnline(camera.Id), presence.IsPublishing(camera.Id)));

    [LoggerMessage(Level = LogLevel.Information, Message = "Device {DeviceId} ({Role}) connected on {ConnectionId}")]
    private static partial void LogConnected(ILogger logger, Guid deviceId, string? role, string connectionId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Device {DeviceId} disconnected from {ConnectionId}: {Reason}")]
    private static partial void LogDisconnected(ILogger logger, Guid deviceId, string connectionId, string? reason);
}
