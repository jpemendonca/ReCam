using Recam.Web.Api;

namespace Recam.Web.Realtime;

/// <summary>The browser Monitor's connection to /hubs/devices. Faked in tests.</summary>
public interface IDeviceHub
{
    bool Connected { get; }

    event Action<CameraInfo>? CameraStatusChanged;

    event Action<Guid, bool>? TorchChanged;

    event Action<Guid>? CameraRemoved;

    /// <summary>A device paired, left, came online or went offline: the device list is stale.</summary>
    event Action? DevicesChanged;

    /// <summary>
    /// <see cref="Connected"/> changed: the connection opened, dropped or came back. Messages sent
    /// while it was down are lost, so listeners reload what they show.
    /// </summary>
    event Action? ConnectedChanged;

    /// <summary>Starts the connection once; later calls do nothing.</summary>
    Task StartAsync(CancellationToken cancellationToken);

    Task<HubCallResult> WatchCameraAsync(Guid cameraId);

    Task<HubCallResult> UnwatchCameraAsync(Guid cameraId);

    Task<HubCallResult> SetTorchAsync(Guid cameraId, bool torchOn);

    Task<HubCallResult> SetRecordingAsync(Guid cameraId, bool enabled);
}
