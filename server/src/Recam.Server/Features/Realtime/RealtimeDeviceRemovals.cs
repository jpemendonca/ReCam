using Microsoft.AspNetCore.SignalR;
using Recam.Server.Infrastructure.Realtime;

namespace Recam.Server.Features.Realtime;

public sealed class RealtimeDeviceRemovals(
    DeviceConnections connections, IHubContext<DeviceHub, IDeviceClient> hub) : IDeviceRemovals
{
    public async Task DeviceRemovedAsync(Guid deviceId, bool wasCamera)
    {
        connections.AbortAll(deviceId);
        if (wasCamera)
        {
            await hub.Clients.Group(DeviceHub.ViewersGroup).CameraRemoved(deviceId);
        }
    }
}
