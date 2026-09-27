namespace Recam.Server.Infrastructure.Realtime;

/// <summary>
/// Tells the Monitors their device list changed (a device paired, left, came online or went
/// offline), so the Devices screen reloads. Implemented by the realtime feature.
/// </summary>
public interface IDeviceListChanges
{
    Task DevicesChangedAsync();
}
