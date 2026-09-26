using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Recam.Web.Live;

/// <summary>The WebRTC part lives in wwwroot/js/whep.js; this class only drives it.</summary>
public sealed class JsLiveVideo(IJSRuntime js) : ILiveVideo
{
    private IJSObjectReference? _module;
    private DotNetObjectReference<JsLiveVideo>? _listener;
    private int _session;

    public event Action? Ended;

    public async Task<bool> StartAsync(ElementReference video, Guid cameraId)
    {
        _module ??= await js.InvokeAsync<IJSObjectReference>("import", "./js/whep.js");
        _listener ??= DotNetObjectReference.Create(this);
        _session = await _module.InvokeAsync<int>("start", video, cameraId, _listener);
        return _session != 0;
    }

    public async Task StopAsync()
    {
        if (_module is not null && _session != 0)
        {
            await _module.InvokeVoidAsync("stop", _session);
        }

        _session = 0;
    }

    public async Task<bool> SetMutedAsync(ElementReference video, bool muted)
    {
        _module ??= await js.InvokeAsync<IJSObjectReference>("import", "./js/whep.js");
        return await _module.InvokeAsync<bool>("setMuted", video, muted);
    }

    [JSInvokable]
    public void OnEnded() => Ended?.Invoke();

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        if (_module is not null)
        {
            await _module.DisposeAsync();
        }

        _listener?.Dispose();
    }
}
