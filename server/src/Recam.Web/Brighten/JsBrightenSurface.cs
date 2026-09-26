using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Recam.Web.Brighten;

/// <summary>Drives wwwroot/js/brighten.js.</summary>
public sealed class JsBrightenSurface(IJSRuntime js) : IBrightenSurface, IAsyncDisposable
{
    private IJSObjectReference? _module;

    public async Task<ImageAdjustment> LoadAsync(Guid cameraId) =>
        ImageAdjustment.Decode(await (await ModuleAsync()).InvokeAsync<string?>("load", cameraId.ToString("N")));

    public async Task SaveAsync(Guid cameraId, ImageAdjustment adjustment) =>
        await (await ModuleAsync()).InvokeVoidAsync("save", cameraId.ToString("N"), adjustment.IsNormal ? null : adjustment.Encode());

    public async Task ApplyAsync(ElementReference video, ImageAdjustment adjustment) =>
        await (await ModuleAsync()).InvokeVoidAsync("apply", video, adjustment.Brightness, adjustment.Contrast);

    public async ValueTask DisposeAsync()
    {
        if (_module is not null)
        {
            await _module.DisposeAsync();
        }
    }

    private async Task<IJSObjectReference> ModuleAsync() =>
        _module ??= await js.InvokeAsync<IJSObjectReference>("import", "./js/brighten.js");
}
