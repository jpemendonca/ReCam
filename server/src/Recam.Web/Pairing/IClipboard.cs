namespace Recam.Web.Pairing;

public interface IClipboard
{
    /// <summary>False when the browser refused, as some do outside a click or without permission.</summary>
    Task<bool> WriteAsync(string text);
}
