using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Recam.Web.Recordings;

/// <summary>Drives wwwroot/js/clock.js.</summary>
public sealed class JsVideoClock(IJSRuntime js) : IVideoClock, IAsyncDisposable
{
    private IJSObjectReference? _module;

    public async Task FollowAsync(ElementReference video, ElementReference label, DateTime fileStart)
    {
        _module ??= await js.InvokeAsync<IJSObjectReference>("import", "./js/clock.js");
        await _module.InvokeVoidAsync("follow", video, label, fileStart.TimeOfDay.TotalSeconds);
    }

    public async ValueTask DisposeAsync()
    {
        if (_module is not null)
        {
            await _module.DisposeAsync();
        }
    }
}
