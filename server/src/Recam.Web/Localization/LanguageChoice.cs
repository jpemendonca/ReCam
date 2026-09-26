using System.Globalization;

namespace Recam.Web.Localization;

/// <summary>
/// The language the Monitor shows: the browser's, or one the person picked in Settings. The
/// choice lives in this browser's localStorage and is applied before the app starts, as Blazor
/// sets the culture only once.
/// </summary>
public static class LanguageChoice
{
    public const string StorageKey = "recam.language";
    public const string Browser = "";
    public const string Portuguese = "pt";
    public const string English = "en";

    /// <summary>The culture to use for a saved choice; null keeps the browser's.</summary>
    public static CultureInfo? CultureFor(string? saved) => saved switch
    {
        Portuguese => CultureInfo.GetCultureInfo("pt-BR"),
        English => CultureInfo.GetCultureInfo("en-US"),
        _ => null,
    };
}
