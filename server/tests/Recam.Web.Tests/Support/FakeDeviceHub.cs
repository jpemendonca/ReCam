using Recam.Web.Api;
using Recam.Web.Realtime;

namespace Recam.Web.Tests.Support;

/// <summary>A hub in memory: records the calls and lets tests send the server's messages.</summary>
public sealed class FakeDeviceHub : IDeviceHub
{
    public bool Connected { get; private set; }

    public List<string> Calls { get; } = [];

    /// <summary>What SetTorch answers; the server refuses while the camera is not publishing.</summary>
    public bool TorchAccepted { get; set; } = true;

    public bool RecordingAccepted { get; set; } = true;

    public event Action<CameraInfo>? CameraStatusChanged;

    public event Action<Guid, bool>? TorchChanged;

    public event Action<Guid>? CameraRemoved;

    public event Action? ConnectedChanged;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!Connected)
        {
            Connected = true;
            ConnectedChanged?.Invoke();
        }

        return Task.CompletedTask;
    }

    public Task<HubCallResult> WatchCameraAsync(Guid cameraId) => Record($"WatchCamera {cameraId}", true);

    public Task<HubCallResult> UnwatchCameraAsync(Guid cameraId) => Record($"UnwatchCamera {cameraId}", true);

    public Task<HubCallResult> SetTorchAsync(Guid cameraId, bool torchOn) => Record($"SetTorch {cameraId} {torchOn}", TorchAccepted);

    public Task<HubCallResult> SetRecordingAsync(Guid cameraId, bool enabled) =>
        Record($"SetRecording {cameraId} {enabled}", RecordingAccepted);

    public void SendStatus(CameraInfo camera) => CameraStatusChanged?.Invoke(camera);

    public void SendTorch(Guid cameraId, bool on) => TorchChanged?.Invoke(cameraId, on);

    public void SendRemoved(Guid cameraId) => CameraRemoved?.Invoke(cameraId);

    public void Drop()
    {
        Connected = false;
        ConnectedChanged?.Invoke();
    }

    public void Reconnect()
    {
        Connected = true;
        ConnectedChanged?.Invoke();
    }

    private Task<HubCallResult> Record(string call, bool ok)
    {
        Calls.Add(call);
        return Task.FromResult(ok ? HubCallResult.Success : new HubCallResult(false, "refused", null));
    }
}
