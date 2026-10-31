using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Recam.Web.Recordings;

/// <summary>Drives wwwroot/js/clock.js.</summary>
public sealed class JsVideoClock(IJSRuntime js) : IVideoClock, IAsyncDisposable
{
    private IJSObjectReference? _module;
    private DotNetObjectReference<JsVideoClock>? _listener;
    private DateTime _day;

    public event Action<DateTime>? HeadMoved;

    // Times go to the script as seconds after the file's midnight.
    public async Task FollowAsync(ElementReference video, ElementReference clock, DateTime fileStart)
    {
        _day = fileStart.Date;
        _listener ??= DotNetObjectReference.Create(this);
        await (await ModuleAsync()).InvokeVoidAsync("follow", video, clock, (fileStart - _day).TotalSeconds, _listener);
    }

    public async Task StopAsync() => await (await ModuleAsync()).InvokeVoidAsync("stop");

    [JSInvokable]
    public void OnHead(double secondsOfDay) => HeadMoved?.Invoke(_day.AddSeconds(secondsOfDay));

    public async ValueTask DisposeAsync()
    {
        if (_module is not null)
        {
            await _module.DisposeAsync();
        }

        _listener?.Dispose();
    }

    private async Task<IJSObjectReference> ModuleAsync() =>
        _module ??= await js.InvokeAsync<IJSObjectReference>("import", "./js/clock.js");
}
