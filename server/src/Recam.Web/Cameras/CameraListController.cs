using Recam.Web.Api;
using Recam.Web.Realtime;

namespace Recam.Web.Cameras;

/// <summary>
/// The cameras this Monitor sees, shared by the pages of the browser app. Loads the list from the
/// API and keeps it current with the hub; every (re)connection reloads it, because messages sent
/// while offline are lost. Same behavior as the app's camera list.
/// </summary>
public sealed class CameraListController(IRecamApi api, IDeviceHub hub) : IDisposable
{
    private readonly SemaphoreSlim _startLock = new(1, 1);
    private List<CameraInfo> _cameras = [];
    private bool _started;

    public CameraListState State { get; private set; } = CameraListState.Loading;

    public IReadOnlyList<CameraInfo> Cameras => _cameras;

    public bool Connected => hub.Connected;

    public event Action? Changed;

    /// <summary>Connects and loads once; later calls only wait for the first one.</summary>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _startLock.WaitAsync(cancellationToken);
        try
        {
            if (_started)
            {
                return;
            }

            _started = true;
            hub.CameraStatusChanged += OnStatusChanged;
            hub.CameraRemoved += OnCameraRemoved;
            hub.ConnectedChanged += OnConnectedChanged;
            await ConnectAsync(cancellationToken);
            await RefreshAsync(cancellationToken);
        }
        finally
        {
            _startLock.Release();
        }
    }

    /// <summary>Reloads from the API, and retries the hub when it never connected.</summary>
    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        try
        {
            var cameras = await api.GetCamerasAsync(cancellationToken);
            if (cameras is null)
            {
                State = CameraListState.SignedOut;
            }
            else
            {
                _cameras = Sorted(cameras);
                State = CameraListState.Loaded;
            }
        }
        catch (HttpRequestException)
        {
            if (State != CameraListState.Loaded)
            {
                State = CameraListState.Failed;
            }
        }

        Changed?.Invoke();
    }

    /// <summary>Retries after the first load failed.</summary>
    public async Task RetryAsync(CancellationToken cancellationToken)
    {
        await ConnectAsync(cancellationToken);
        await RefreshAsync(cancellationToken);
    }

    public CameraInfo? Camera(Guid cameraId) => _cameras.FirstOrDefault(camera => camera.Id == cameraId);

    /// <summary>
    /// Turns "record always" on or off. False when the server refuses; the list shows the new
    /// state once the server confirms it.
    /// </summary>
    public async Task<bool> SetRecordingAsync(Guid cameraId, bool enabled) =>
        (await hub.SetRecordingAsync(cameraId, enabled)).Ok;

    private async Task ConnectAsync(CancellationToken cancellationToken)
    {
        try
        {
            await hub.StartAsync(cancellationToken);
        }
        catch (HttpRequestException)
        {
            // The list still loads from the API; the retry button tries the hub again.
        }
    }

    private void OnStatusChanged(CameraInfo camera)
    {
        if (State != CameraListState.Loaded)
        {
            return;
        }

        _cameras = Sorted([.. _cameras.Where(existing => existing.Id != camera.Id), camera]);
        Changed?.Invoke();
    }

    private void OnCameraRemoved(Guid cameraId)
    {
        _cameras = [.. _cameras.Where(camera => camera.Id != cameraId)];
        Changed?.Invoke();
    }

    private void OnConnectedChanged()
    {
        Changed?.Invoke();
        if (hub.Connected && State == CameraListState.Loaded)
        {
            _ = RefreshAsync(CancellationToken.None);
        }
    }

    public void Dispose()
    {
        hub.CameraStatusChanged -= OnStatusChanged;
        hub.CameraRemoved -= OnCameraRemoved;
        hub.ConnectedChanged -= OnConnectedChanged;
        _startLock.Dispose();
    }

    private static List<CameraInfo> Sorted(IEnumerable<CameraInfo> cameras) =>
        [.. cameras.OrderBy(camera => camera.Name, StringComparer.CurrentCultureIgnoreCase)];
}
