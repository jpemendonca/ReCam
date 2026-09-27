using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Recam.Server.Domain;
using Recam.Server.Infrastructure.Persistence;
using Recam.Server.Infrastructure.Recordings;

namespace Recam.Server.Features.Realtime;

/// <summary>
/// Every few seconds, tells the Monitors when a camera's recording state changed: the first file
/// arrived, files stopped, the camera waits too long. Nothing else raises these, since they come
/// from the disk.
/// </summary>
public sealed class RecordingStateWorker(
    IDbContextFactory<RecamDbContext> databaseFactory,
    RecordingStateTracker tracker,
    IHubContext<DeviceHub, IDeviceClient> hub,
    TimeProvider timeProvider) : BackgroundService
{
    public static readonly TimeSpan Interval = TimeSpan.FromSeconds(5);

    public async Task CheckAsync(CancellationToken cancellationToken)
    {
        await using var database = await databaseFactory.CreateDbContextAsync(cancellationToken);
        var cameras = await database.Devices.AsNoTracking()
            .Where(device => device.Role == DeviceRole.Camera && device.RevokedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var camera in cameras)
        {
            var status = tracker.StatusOf(camera);
            if (tracker.IsNews(camera.Id, status.RecordingState))
            {
                await hub.Clients.Group(DeviceHub.ViewersGroup).CameraStatusChanged(status);
            }
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval, timeProvider);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await CheckAsync(stoppingToken);
        }
    }
}
