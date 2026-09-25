using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Recam.Server.Infrastructure.Persistence;
using Recam.Server.Infrastructure.Presence;

namespace Recam.Server.Features.Realtime;

/// <summary>
/// Who is watching each camera. The first watcher makes the camera publish; when the last one
/// leaves, the camera stops after <see cref="StopGrace"/>, so a quick reopen does not restart
/// the stream. Every change in the number of watchers is sent to the camera and kept in
/// <see cref="DevicePresence"/>, where other features read it.
/// </summary>
public sealed class WatchLeases(
    IHubContext<DeviceHub, IDeviceClient> hub,
    DevicePresence presence,
    IDbContextFactory<RecamDbContext> databaseFactory,
    TimeProvider timeProvider)
{
    public static readonly TimeSpan StopGrace = TimeSpan.FromSeconds(30);

    private readonly Lock _lock = new();
    private readonly Dictionary<Guid, HashSet<string>> _watchers = [];
    private readonly Dictionary<Guid, CancellationTokenSource> _pendingStops = [];

    public async Task WatchAsync(string connectionId, Guid cameraId)
    {
        bool added;
        bool startNeeded;
        int count;
        lock (_lock)
        {
            if (!_watchers.TryGetValue(cameraId, out var connections))
            {
                connections = [];
                _watchers[cameraId] = connections;
            }

            added = connections.Add(connectionId);
            var firstWatcher = added && connections.Count == 1;
            var stopWasPending = CancelPendingStop(cameraId);
            startNeeded = firstWatcher && !stopWasPending;
            count = connections.Count;
            presence.SetWatchers(cameraId, count);
        }

        if (startNeeded)
        {
            await hub.Clients.User(DeviceHub.UserId(cameraId)).StartPublishing();
        }

        if (added)
        {
            await NotifyCountAsync(cameraId, count);
        }
    }

    public async Task UnwatchAsync(string connectionId, Guid cameraId)
    {
        int? remaining;
        lock (_lock)
        {
            remaining = ReleaseLease(connectionId, cameraId);
        }

        if (remaining is { } count)
        {
            await NotifyCountAsync(cameraId, count);
        }
    }

    public async Task RemoveConnectionAsync(string connectionId)
    {
        List<(Guid CameraId, int Count)> changed = [];
        lock (_lock)
        {
            foreach (var cameraId in _watchers.Where(pair => pair.Value.Contains(connectionId)).Select(pair => pair.Key).ToList())
            {
                if (ReleaseLease(connectionId, cameraId) is { } count)
                {
                    changed.Add((cameraId, count));
                }
            }
        }

        foreach (var (cameraId, count) in changed)
        {
            await NotifyCountAsync(cameraId, count);
        }
    }

    /// <summary>
    /// Moves a camera to the path that matches its recording state. A publishing camera restarts,
    /// because the path is chosen when it publishes; it keeps publishing while it records or
    /// someone watches, and stops otherwise.
    /// </summary>
    public async Task RecordingChangedAsync(Guid cameraId, bool recording)
    {
        var camera = hub.Clients.User(DeviceHub.UserId(cameraId));
        var keepPublishing = recording || presence.Watchers(cameraId) > 0;
        if (presence.IsPublishing(cameraId))
        {
            await camera.StopPublishing();
            if (keepPublishing)
            {
                await camera.StartPublishing();
            }
        }
        else if (recording)
        {
            await camera.StartPublishing();
        }
    }

    private Task NotifyCountAsync(Guid cameraId, int count) =>
        hub.Clients.User(DeviceHub.UserId(cameraId)).WatchersChanged(count);

    /// <summary>Returns the watchers left, or null when the connection held no lease.</summary>
    private int? ReleaseLease(string connectionId, Guid cameraId)
    {
        if (!_watchers.TryGetValue(cameraId, out var connections) || !connections.Remove(connectionId))
        {
            return null;
        }

        presence.SetWatchers(cameraId, connections.Count);
        if (connections.Count > 0)
        {
            return connections.Count;
        }

        _watchers.Remove(cameraId);
        var cancellation = new CancellationTokenSource();
        _pendingStops[cameraId] = cancellation;
        _ = StopAfterGraceAsync(cameraId, cancellation);
        return 0;
    }

    private bool CancelPendingStop(Guid cameraId)
    {
        if (!_pendingStops.Remove(cameraId, out var cancellation))
        {
            return false;
        }

        cancellation.Cancel();
        return true;
    }

    private async Task StopAfterGraceAsync(Guid cameraId, CancellationTokenSource cancellation)
    {
        await Task.Delay(StopGrace, timeProvider, cancellation.Token)
            .ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        var stillPending = false;
        lock (_lock)
        {
            if (!cancellation.IsCancellationRequested
                && _pendingStops.TryGetValue(cameraId, out var current)
                && current == cancellation)
            {
                _pendingStops.Remove(cameraId);
                stillPending = true;
            }
        }

        cancellation.Dispose();
        if (stillPending && !await IsRecordingAsync(cameraId))
        {
            await hub.Clients.User(DeviceHub.UserId(cameraId)).StopPublishing();
        }
    }

    // A camera that records keeps publishing when the last viewer leaves.
    private async Task<bool> IsRecordingAsync(Guid cameraId)
    {
        await using var database = await databaseFactory.CreateDbContextAsync();
        return await database.Devices
            .Where(device => device.Id == cameraId)
            .Select(device => device.RecordingEnabled)
            .SingleOrDefaultAsync();
    }
}
