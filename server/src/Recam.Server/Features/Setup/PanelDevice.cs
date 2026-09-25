using Recam.Server.Domain;
using Recam.Server.Infrastructure.Presence;

namespace Recam.Server.Features.Setup;

/// <summary>One row of the read-only panel: stored data plus live presence.</summary>
public sealed record PanelDevice(
    Guid Id,
    string Name,
    DeviceRole Role,
    bool Online,
    bool Publishing,
    int Watchers,
    int? BatteryLevel,
    bool? IsCharging)
{
    public static PanelDevice From(Device device, DevicePresence presence) =>
        new(
            device.Id,
            device.Name,
            device.Role,
            presence.IsOnline(device.Id),
            presence.IsPublishing(device.Id),
            presence.Watchers(device.Id),
            device.BatteryLevel,
            device.IsCharging);
}
