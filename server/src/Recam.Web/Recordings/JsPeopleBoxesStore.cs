using Microsoft.JSInterop;

namespace Recam.Web.Recordings;

/// <summary>The choice in localStorage. A browser that blocks storage just shows the boxes.</summary>
public sealed class JsPeopleBoxesStore(IJSRuntime js) : IPeopleBoxesStore
{
    private const string StorageKey = "recam.people-boxes";

    public async Task<bool> ReadAsync()
    {
        try
        {
            return await js.InvokeAsync<string?>("localStorage.getItem", StorageKey) != "off";
        }
        catch (JSException)
        {
            return true;
        }
    }

    public async Task SaveAsync(bool show)
    {
        try
        {
            await js.InvokeVoidAsync("localStorage.setItem", StorageKey, show ? "on" : "off");
        }
        catch (JSException)
        {
            // Without storage the choice lasts until the page closes.
        }
    }
}
