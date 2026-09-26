using Microsoft.AspNetCore.Components;
using Recam.Web.Realtime;

namespace Recam.Web.Live;

/// <summary>
/// One live view in the browser, as in the app: holds a watch lease on the camera while open,
/// retries WHEP while the camera is still opening (the stream only exists once it publishes),
/// and switches the camera's torch, showing what the camera reports about it.
/// </summary>
public sealed class LiveController(IDeviceHub hub, ILiveVideo video) : IAsyncDisposable
{
    public const int MaxAttempts = 20;

    private ElementReference _element;
    private Guid _cameraId;
    private bool _open;
    private bool _closed;
    private int _generation;

    public LiveState State { get; private set; } = LiveState.Connecting;

    public bool TorchOn { get; private set; }

    /// <summary>Starts true: browsers only autoplay silent video.</summary>
    public bool Muted { get; private set; } = true;

    /// <summary>Wait between WHEP attempts while the camera opens.</summary>
    public TimeSpan RetryInterval { get; set; } = TimeSpan.FromSeconds(1);

    public event Action? Changed;

    /// <summary>Opens the live video; <paramref name="torchOn"/> is the torch as the server last heard it.</summary>
    public async Task OpenAsync(Guid cameraId, ElementReference element, bool torchOn = false)
    {
        _cameraId = cameraId;
        TorchOn = torchOn;
        _element = element;
        _open = true;
        hub.TorchChanged += OnTorchChanged;
        video.Ended += OnEnded;
        await hub.WatchCameraAsync(cameraId);
        await ConnectAsync();
    }

    /// <summary>Tries again after a failure, keeping the same lease.</summary>
    public async Task RetryAsync()
    {
        await hub.WatchCameraAsync(_cameraId);
        await ConnectAsync();
    }

    /// <summary>
    /// Asks the camera to switch its torch. False when the server refuses (the camera is not
    /// sending video). The new state arrives later, as the camera reports it.
    /// </summary>
    public async Task<bool> SetTorchAsync(bool on) => (await hub.SetTorchAsync(_cameraId, on)).Ok;

    /// <summary>Turns the camera's sound on or off.</summary>
    public async Task ToggleMutedAsync()
    {
        Muted = !Muted;
        await video.SetMutedAsync(_element, Muted);
        Changed?.Invoke();
    }

    public async ValueTask DisposeAsync()
    {
        if (_closed)
        {
            return;
        }

        _closed = true;
        _generation++;
        if (!_open)
        {
            return;
        }

        hub.TorchChanged -= OnTorchChanged;
        video.Ended -= OnEnded;
        await video.DisposeAsync();
        await hub.UnwatchCameraAsync(_cameraId);
    }

    private async Task ConnectAsync()
    {
        var generation = ++_generation;
        SetState(LiveState.Connecting);
        for (var attempt = 0; attempt < MaxAttempts; attempt++)
        {
            if (_closed || generation != _generation)
            {
                return;
            }

            if (await video.StartAsync(_element, _cameraId))
            {
                if (!_closed && generation == _generation)
                {
                    SetState(LiveState.Playing);
                }

                return;
            }

            await Task.Delay(RetryInterval);
        }

        if (!_closed && generation == _generation)
        {
            SetState(LiveState.Failed);
        }
    }

    private void OnTorchChanged(Guid cameraId, bool on)
    {
        if (cameraId != _cameraId || on == TorchOn)
        {
            return;
        }

        TorchOn = on;
        Changed?.Invoke();
    }

    private void OnEnded() => _ = RetryAfterDropAsync();

    private async Task RetryAfterDropAsync()
    {
        if (_closed)
        {
            return;
        }

        await video.StopAsync();
        await ConnectAsync();
    }

    private void SetState(LiveState state)
    {
        State = state;
        Changed?.Invoke();
    }
}
