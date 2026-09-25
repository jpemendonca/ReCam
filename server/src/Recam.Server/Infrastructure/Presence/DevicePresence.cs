using System.Collections.Concurrent;

namespace Recam.Server.Infrastructure.Presence;

/// <summary>
/// Which devices have at least one live hub connection. In memory: a restart starts everyone
/// offline, and devices reconnect on their own.
/// </summary>
public sealed class DevicePresence
{
    private readonly ConcurrentDictionary<Guid, int> _connections = new();

    /// <summary>Returns true when this is the device's first connection (it just came online).</summary>
    public bool Connect(Guid deviceId) => _connections.AddOrUpdate(deviceId, 1, (_, count) => count + 1) == 1;

    /// <summary>Returns true when this was the device's last connection (it just went offline).</summary>
    public bool Disconnect(Guid deviceId)
    {
        while (_connections.TryGetValue(deviceId, out var count))
        {
            if (count <= 1)
            {
                if (_connections.TryRemove(new KeyValuePair<Guid, int>(deviceId, count)))
                {
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
}
