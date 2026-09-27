using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Recam.Web.Api;

namespace Recam.Web.Realtime;

/// <summary>
/// The hub connection, authenticated by the device cookie the browser sends on its own. The web
/// header goes on the HTTP requests SignalR makes (SPECS.md 5.4). Reconnects forever with the
/// app's backoff: 1, 2, 4, 8, 16, then every 30 seconds.
/// </summary>
public sealed class SignalRDeviceHub : IDeviceHub, IAsyncDisposable
{
    private readonly HubConnection _connection;
    private readonly SemaphoreSlim _startLock = new(1, 1);
    private bool _started;

    public SignalRDeviceHub(NavigationManager navigation)
    {
        _connection = new HubConnectionBuilder()
            .WithUrl(navigation.ToAbsoluteUri("hubs/devices"), options => options.Headers[HttpRecamApi.WebHeader] = "1")
            .WithAutomaticReconnect(new Backoff())
            .Build();
        _connection.On<CameraInfo>("CameraStatusChanged", camera => CameraStatusChanged?.Invoke(camera));
        _connection.On<Guid, bool>("TorchChanged", (cameraId, on) => TorchChanged?.Invoke(cameraId, on));
        _connection.On<Guid>("CameraRemoved", cameraId => CameraRemoved?.Invoke(cameraId));
        _connection.On("DevicesChanged", () => DevicesChanged?.Invoke());
        _connection.Reconnecting += _ => Notify();
        _connection.Reconnected += _ => Notify();
        _connection.Closed += _ => Notify();
    }

    public bool Connected => _connection.State == HubConnectionState.Connected;

    public event Action<CameraInfo>? CameraStatusChanged;

    public event Action<Guid, bool>? TorchChanged;

    public event Action<Guid>? CameraRemoved;

    public event Action? DevicesChanged;

    public event Action? ConnectedChanged;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _startLock.WaitAsync(cancellationToken);
        try
        {
            if (_started)
            {
                return;
            }

            await _connection.StartAsync(cancellationToken);
            _started = true;
        }
        finally
        {
            _startLock.Release();
        }

        ConnectedChanged?.Invoke();
    }

    public Task<HubCallResult> WatchCameraAsync(Guid cameraId) => InvokeAsync("WatchCamera", cameraId);

    public Task<HubCallResult> UnwatchCameraAsync(Guid cameraId) => InvokeAsync("UnwatchCamera", cameraId);

    public Task<HubCallResult> SetTorchAsync(Guid cameraId, bool torchOn) => InvokeAsync("SetTorch", cameraId, torchOn);

    public Task<HubCallResult> SetRecordingAsync(Guid cameraId, bool enabled) => InvokeAsync("SetRecording", cameraId, enabled);

    public async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync();
        _startLock.Dispose();
    }

    private async Task<HubCallResult> InvokeAsync(string method, params object[] arguments)
    {
        if (!Connected)
        {
            return new HubCallResult(false, "hub.disconnected", null);
        }

        try
        {
            return await _connection.InvokeCoreAsync<HubCallResult>(method, arguments);
        }
        catch (Exception exception) when (exception is HubException or InvalidOperationException or IOException)
        {
            // The connection dropped between the check and the call.
            return new HubCallResult(false, "hub.failed", exception.Message);
        }
    }

    private Task Notify()
    {
        ConnectedChanged?.Invoke();
        return Task.CompletedTask;
    }

    private sealed class Backoff : IRetryPolicy
    {
        private static readonly TimeSpan[] Steps = [TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4),
            TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(16)];

        public TimeSpan? NextRetryDelay(RetryContext retryContext) =>
            retryContext.PreviousRetryCount < Steps.Length ? Steps[retryContext.PreviousRetryCount] : TimeSpan.FromSeconds(30);
    }
}
