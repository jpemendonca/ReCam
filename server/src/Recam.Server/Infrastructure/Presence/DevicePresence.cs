using System.Collections.Concurrent;

namespace Recam.Server.Infrastructure.Presence;

/// <summary>
/// Which devices have at least one live hub connection, which cameras publish and how many
/// viewers watch each one. In memory: a restart starts everyone offline, and devices reconnect
/// on their own.
/// </summary>
public sealed class DevicePresence
{
    private readonly ConcurrentDictionary<Guid, int> _connections = new();
    private readonly ConcurrentDictionary<Guid, bool> _onlineCameras = new();
    private readonly ConcurrentDictionary<Guid, bool> _publishing = new();
    private readonly ConcurrentDictionary<Guid, int> _watchers = new();
    private readonly ConcurrentDictionary<Guid, List<DateTimeOffset>> _torchChanges = new();

    /// <summary>How long torch changes are remembered, for motion detection to ignore them.</summary>
    public static readonly TimeSpan TorchMemory = TimeSpan.FromDays(3);

    /// <summary>Returns true when this is the device's first connection (it just came online).</summary>
    public bool Connect(Guid deviceId, bool isCamera)
    {
        var cameOnline = _connections.AddOrUpdate(deviceId, 1, (_, count) => count + 1) == 1;
        if (cameOnline && isCamera)
        {
            _onlineCameras[deviceId] = true;
        }

        return cameOnline;
    }

    /// <summary>Returns true when this was the device's last connection (it just went offline).</summary>
    public bool Disconnect(Guid deviceId)
    {
        while (_connections.TryGetValue(deviceId, out var count))
        {
            if (count <= 1)
            {
                if (_connections.TryRemove(new KeyValuePair<Guid, int>(deviceId, count)))
                {
                    _publishing.TryRemove(deviceId, out _);
                    _onlineCameras.TryRemove(deviceId, out _);
                    return true;
                }
            }
            else if (_connections.TryUpdate(deviceId, count - 1, count))
            {
                return false;
            }
        }

        return false;
    }

    public bool IsOnline(Guid deviceId) => _connections.ContainsKey(deviceId);

    /// <summary>What the camera last reported; cleared when it goes offline.</summary>
    public void SetPublishing(Guid cameraId, bool publishing)
    {
        if (publishing)
        {
            _publishing[cameraId] = true;
        }
        else
        {
            _publishing.TryRemove(cameraId, out _);
        }
    }

    public bool IsPublishing(Guid cameraId) => _publishing.ContainsKey(cameraId);

    /// <summary>Set by whoever owns the watch leases, each time the count changes.</summary>
    public void SetWatchers(Guid cameraId, int count)
    {
        if (count > 0)
        {
            _watchers[cameraId] = count;
        }
        else
        {
            _watchers.TryRemove(cameraId, out _);
        }
    }

    public int Watchers(Guid cameraId) => _watchers.GetValueOrDefault(cameraId);

    /// <summary>When the camera said its torch switched. The picture jumps then; that is not motion.</summary>
    public void RecordTorchChange(Guid cameraId, DateTimeOffset at)
    {
        var changes = _torchChanges.GetOrAdd(cameraId, _ => []);
        lock (changes)
        {
            changes.RemoveAll(change => change < at - TorchMemory);
            changes.Add(at);
        }
    }

    /// <summary>The camera's torch changes of the last days (in memory: a restart forgets them).</summary>
    public IReadOnlyList<DateTimeOffset> TorchChanges(Guid cameraId)
    {
        if (!_torchChanges.TryGetValue(cameraId, out var changes))
        {
            return [];
        }

        lock (changes)
        {
            return [.. changes];
        }
    }

    /// <summary>Cameras with at least one live hub connection.</summary>
    public int OnlineCameras => _onlineCameras.Count;

    /// <summary>Cameras that last reported they are sending video.</summary>
    public int PublishingCameras => _publishing.Count;

    /// <summary>Live views across all cameras; one viewer watching two cameras counts twice.</summary>
    public int ActiveViews => _watchers.Values.Sum();
}
