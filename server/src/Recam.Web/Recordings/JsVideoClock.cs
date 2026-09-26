using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Recam.Web.Recordings;

/// <summary>Drives wwwroot/js/clock.js.</summary>
public sealed class JsVideoClock(IJSRuntime js) : IVideoClock, IAsyncDisposable
{
    private IJSObjectReference? _module;
    private DotNetObjectReference<JsVideoClock>? _listener;
    private DateTime _day;

    public event Action<DateTime>? HeadLeftWindow;

    public async Task FollowAsync(ElementReference video, ElementReference clock, ElementReference line, DateTime fileStart) =>
        await (await ModuleAsync()).InvokeVoidAsync("follow", video, clock, line, (fileStart - _day).TotalSeconds);

    public async Task StopAsync() => await (await ModuleAsync()).InvokeVoidAsync("stop");

    // Times go to the script as seconds after the day's midnight.
    public async Task ShowWindowAsync(TimelineWindow window, double width)
    {
        _day = window.Day;
        _listener ??= DotNetObjectReference.Create(this);
        await (await ModuleAsync()).InvokeVoidAsync(
            "showWindow", (window.Start - _day).TotalSeconds, (window.End - _day).TotalSeconds, width, _listener);
    }

    [JSInvokable]
    public void OnHeadLeft(double secondsOfDay) => HeadLeftWindow?.Invoke(_day.AddSeconds(secondsOfDay));

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
