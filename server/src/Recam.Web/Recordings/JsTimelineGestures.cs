using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Recam.Web.Recordings;

/// <summary>Drives wwwroot/js/timeline.js.</summary>
public sealed class JsTimelineGestures(IJSRuntime js) : ITimelineGestures, IAsyncDisposable
{
    private IJSObjectReference? _module;
    private DotNetObjectReference<JsTimelineGestures>? _listener;

    public event Action<double>? Tapped;

    public event Action<double>? Dragging;

    public event Action<double>? Dropped;

    public async Task AttachAsync(ElementReference bar)
    {
        _module ??= await js.InvokeAsync<IJSObjectReference>("import", "./js/timeline.js");
        _listener ??= DotNetObjectReference.Create(this);
        await _module.InvokeVoidAsync("attach", bar, _listener);
    }

    [JSInvokable]
    public void OnTap(double fraction) => Tapped?.Invoke(fraction);

    [JSInvokable]
    public void OnDrag(double fraction) => Dragging?.Invoke(fraction);

    [JSInvokable]
    public void OnDrop(double fraction) => Dropped?.Invoke(fraction);

    public async ValueTask DisposeAsync()
    {
        if (_module is not null)
        {
            await _module.DisposeAsync();
        }

        _listener?.Dispose();
    }
}
