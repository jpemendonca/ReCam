using Microsoft.AspNetCore.SignalR;

namespace Recam.Server.Features.Realtime;

/// <summary>The open hub connections of each device, so a removed device can be cut off.</summary>
public sealed class DeviceConnections
{
    private readonly Lock _lock = new();
    private readonly Dictionary<Guid, Dictionary<string, HubCallerContext>> _byDevice = [];

    public void Add(Guid deviceId, HubCallerContext context)
    {
        lock (_lock)
        {
            if (!_byDevice.TryGetValue(deviceId, out var connections))
            {
                connections = [];
                _byDevice[deviceId] = connections;
            }

            connections[context.ConnectionId] = context;
        }
    }

    public void Remove(Guid deviceId, string connectionId)
    {
        lock (_lock)
        {
            if (_byDevice.TryGetValue(deviceId, out var connections)
                && connections.Remove(connectionId)
                && connections.Count == 0)
            {
                _byDevice.Remove(deviceId);
            }
        }
    }

    /// <summary>Closes every connection of the device; reconnecting fails, because it is revoked.</summary>
    public void AbortAll(Guid deviceId)
    {
        List<HubCallerContext> toAbort;
        lock (_lock)
        {
            toAbort = _byDevice.TryGetValue(deviceId, out var connections) ? [.. connections.Values] : [];
        }

        foreach (var context in toAbort)
        {
            context.Abort();
        }
    }
}
