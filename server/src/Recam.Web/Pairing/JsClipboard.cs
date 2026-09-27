using Microsoft.JSInterop;

namespace Recam.Web.Pairing;

public sealed class JsClipboard(IJSRuntime js) : IClipboard
{
    public async Task<bool> WriteAsync(string text)
    {
        try
        {
            await js.InvokeVoidAsync("navigator.clipboard.writeText", text);
            return true;
        }
        catch (JSException)
        {
            return false;
        }
    }
}
