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
public sealed class DeviceHub(
    IDbContextFactory<RecamDbContext> databaseFactory,
    DevicePresence presence,
    TimeProvider timeProvider) : Hub<IDeviceClient>
{
    public const string Path = "/hubs/devices";
    public const string ViewersGroup = "viewers";

    public override async Task OnConnectedAsync()
    {
        var user = Context.User!;
        if (user.IsInRole(nameof(DeviceRole.Owner)) || user.IsInRole(nameof(DeviceRole.Viewer)))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, ViewersGroup, Context.ConnectionAborted);
        }

        if (presence.Connect(user.GetDeviceId()))
        {
            await PresenceChangedAsync(user.GetDeviceId(), Context.ConnectionAborted);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var deviceId = Context.User!.GetDeviceId();
        if (presence.Disconnect(deviceId))
        {
            await PresenceChangedAsync(deviceId, CancellationToken.None);
        }

        await base.OnDisconnectedAsync(exception);
    }

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
        Clients.Group(ViewersGroup).CameraStatusChanged(camera.ToCameraStatus(presence.IsOnline(camera.Id), publishing: false));
}
