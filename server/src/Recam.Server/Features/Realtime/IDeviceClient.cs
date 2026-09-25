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

    Task TorchChanged(Guid cameraId, bool torchOn);
}
