using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Recam.Web.Recordings;

/// <summary>Drives wwwroot/js/people.js.</summary>
public sealed class JsPeopleOverlay(IJSRuntime js) : IPeopleOverlay, IAsyncDisposable
{
    private IJSObjectReference? _module;
    private DotNetObjectReference<PersonTrack>? _track;

    public async Task FollowAsync(ElementReference video, ElementReference boxes, PersonTrack track)
    {
        _track?.Dispose();
        _track = DotNetObjectReference.Create(track);
        await (await ModuleAsync()).InvokeVoidAsync("follow", video, boxes, _track);
    }

    public async Task StopAsync()
    {
        await (await ModuleAsync()).InvokeVoidAsync("stop");
        _track?.Dispose();
        _track = null;
    }

    public async Task FullScreenAsync(ElementReference player) =>
        await (await ModuleAsync()).InvokeVoidAsync("fullScreen", player);

    public async ValueTask DisposeAsync()
    {
        if (_module is not null)
        {
            await _module.DisposeAsync();
        }

        _track?.Dispose();
    }

    private async Task<IJSObjectReference> ModuleAsync() =>
        _module ??= await js.InvokeAsync<IJSObjectReference>("import", "./js/people.js");
}
