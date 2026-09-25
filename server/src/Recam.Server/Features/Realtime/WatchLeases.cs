using Microsoft.AspNetCore.SignalR;

namespace Recam.Server.Features.Realtime;

/// <summary>
/// Who is watching each camera. The first watcher makes the camera publish; when the last one
/// leaves, the camera stops after <see cref="StopGrace"/>, so a quick reopen does not restart
/// the stream. Every change in the number of watchers is sent to the camera.
/// </summary>
public sealed class WatchLeases(IHubContext<DeviceHub, IDeviceClient> hub, TimeProvider timeProvider)
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

    /// <summary>Open leases on the camera: one per watching connection.</summary>
    public int WatcherCount(Guid cameraId)
    {
        lock (_lock)
        {
            return _watchers.TryGetValue(cameraId, out var connections) ? connections.Count : 0;
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
        if (stillPending)
        {
            await hub.Clients.User(DeviceHub.UserId(cameraId)).StopPublishing();
        }
    }
}
