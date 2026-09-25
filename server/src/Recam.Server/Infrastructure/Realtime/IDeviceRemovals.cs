namespace Recam.Server.Infrastructure.Realtime;

/// <summary>
/// What happens live when a device leaves the server: its open connections drop, and Monitors
/// stop showing it. Implemented by the realtime feature, which owns the connections.
/// </summary>
public interface IDeviceRemovals
{
    Task DeviceRemovedAsync(Guid deviceId, bool wasCamera);
}
