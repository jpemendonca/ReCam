using Microsoft.JSInterop;

namespace Recam.Web.Localization;

/// <summary>The choice in localStorage. A browser that blocks storage just keeps its own language.</summary>
public sealed class JsLanguageStore(IJSRuntime js) : ILanguageStore
{
    public async Task<string> ReadAsync()
    {
        try
        {
            return await js.InvokeAsync<string?>("localStorage.getItem", LanguageChoice.StorageKey) ?? LanguageChoice.Browser;
        }
        catch (JSException)
        {
            return LanguageChoice.Browser;
        }
    }

    public async Task SaveAsync(string choice)
    {
        try
        {
            if (choice == LanguageChoice.Browser)
            {
                await js.InvokeVoidAsync("localStorage.removeItem", LanguageChoice.StorageKey);
            }
            else
            {
                await js.InvokeVoidAsync("localStorage.setItem", LanguageChoice.StorageKey, choice);
            }
        }
        catch (JSException)
        {
            // Without storage the choice cannot last; the page keeps the browser's language.
        }
    }
}
