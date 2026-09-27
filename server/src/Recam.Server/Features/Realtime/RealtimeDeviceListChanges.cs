using Microsoft.AspNetCore.SignalR;
using Recam.Server.Infrastructure.Realtime;

namespace Recam.Server.Features.Realtime;

public sealed class RealtimeDeviceListChanges(IHubContext<DeviceHub, IDeviceClient> hub) : IDeviceListChanges
{
    public Task DevicesChangedAsync() => hub.Clients.Group(DeviceHub.ViewersGroup).DevicesChanged();
}
