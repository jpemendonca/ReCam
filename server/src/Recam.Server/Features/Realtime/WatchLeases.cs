using Microsoft.AspNetCore.SignalR;

namespace Recam.Server.Features.Realtime;

/// <summary>
/// Who is watching each camera. The first watcher makes the camera publish; when the last one
/// leaves, the camera stops after <see cref="StopGrace"/>, so a quick reopen does not restart
/// the stream.
/// </summary>
public sealed class WatchLeases(IHubContext<DeviceHub, IDeviceClient> hub, TimeProvider timeProvider)
{
    public static readonly TimeSpan StopGrace = TimeSpan.FromSeconds(30);

    private readonly Lock _lock = new();
    private readonly Dictionary<Guid, HashSet<string>> _watchers = [];
    private readonly Dictionary<Guid, CancellationTokenSource> _pendingStops = [];

    public async Task WatchAsync(string connectionId, Guid cameraId)
    {
        bool startNeeded;
        lock (_lock)
        {
            if (!_watchers.TryGetValue(cameraId, out var connections))
            {
                connections = [];
                _watchers[cameraId] = connections;
            }

            var firstWatcher = connections.Add(connectionId) && connections.Count == 1;
            var stopWasPending = CancelPendingStop(cameraId);
            startNeeded = firstWatcher && !stopWasPending;
        }

        if (startNeeded)
        {
            await hub.Clients.User(DeviceHub.UserId(cameraId)).StartPublishing();
        }
    }

    public void Unwatch(string connectionId, Guid cameraId)
    {
        lock (_lock)
        {
            ReleaseLease(connectionId, cameraId);
        }
    }

    public void RemoveConnection(string connectionId)
    {
        lock (_lock)
        {
            foreach (var cameraId in _watchers.Where(pair => pair.Value.Contains(connectionId)).Select(pair => pair.Key).ToList())
            {
                ReleaseLease(connectionId, cameraId);
            }
        }
    }

    public bool HasWatchers(Guid cameraId)
    {
        lock (_lock)
        {
            return _watchers.ContainsKey(cameraId);
        }
    }

    private void ReleaseLease(string connectionId, Guid cameraId)
    {
        if (!_watchers.TryGetValue(cameraId, out var connections) || !connections.Remove(connectionId))
        {
            return;
        }

        if (connections.Count > 0)
        {
            return;
        }

        _watchers.Remove(cameraId);
        var cancellation = new CancellationTokenSource();
        _pendingStops[cameraId] = cancellation;
        _ = StopAfterGraceAsync(cameraId, cancellation);
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
