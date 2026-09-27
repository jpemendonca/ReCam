using Recam.Server.Domain;

namespace Recam.Server.Features.Realtime;

/// <summary>Messages the server sends to connected devices (SPECS.md 5.6).</summary>
public interface IDeviceClient
{
    Task CameraStatusChanged(CameraStatus status);

    Task StartPublishing();

    Task StopPublishing();

    /// <summary>How many viewers hold a lease on this camera now.</summary>
    Task WatchersChanged(int count);

    Task SetTorch(bool torchOn);

    /// <summary>Whether this camera records; while it does, it publishes even with nobody watching.</summary>
    Task RecordingChanged(bool recording);

    Task TorchChanged(Guid cameraId, bool torchOn);

    /// <summary>A camera left the server; Monitors drop it from their list.</summary>
    Task CameraRemoved(Guid cameraId);

    /// <summary>The device list changed; the Devices screen loads it again.</summary>
    Task DevicesChanged();
}
