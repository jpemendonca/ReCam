using Microsoft.Net.Http.Headers;

namespace Recam.Server.Infrastructure.Http;

/// <summary>
/// The name a browser gets when it becomes a Monitor, e.g. "Navegador · Chrome no Windows", in
/// the browser's language (Portuguese or English). Shown in the device lists.
/// </summary>
public static class BrowserDeviceName
{
    // Order matters: Edge and Opera also say Chrome, and Chrome also says Safari.
    private static readonly (string Marker, string Name)[] Browsers =
    [
        ("Edg/", "Edge"),
        ("OPR/", "Opera"),
        ("Firefox/", "Firefox"),
        ("Chrome/", "Chrome"),
        ("Safari/", "Safari"),
    ];

    private static readonly (string Marker, string Name)[] Systems =
    [
        ("Windows", "Windows"),
        ("Android", "Android"),
        ("iPhone", "iOS"),
        ("iPad", "iPadOS"),
        ("CrOS", "ChromeOS"),
        ("Mac OS X", "macOS"),
        ("Linux", "Linux"),
    ];

    public static string From(string? userAgent, IList<StringWithQualityHeaderValue> acceptLanguage)
    {
        var portuguese = PrefersPortuguese(acceptLanguage);
        var agent = userAgent ?? string.Empty;
        var browser = Browsers.FirstOrDefault(entry => agent.Contains(entry.Marker, StringComparison.Ordinal)).Name;
        var system = Systems.FirstOrDefault(entry => agent.Contains(entry.Marker, StringComparison.Ordinal)).Name;
        var label = portuguese ? "Navegador" : "Browser";
        return (browser, system) switch
        {
            (null, _) => label,
            (_, null) => $"{label} · {browser}",
            _ => $"{label} · {browser} {(portuguese ? "no" : "on")} {system}",
        };
    }

    /// <summary>The first language the browser names between Portuguese and English; English otherwise.</summary>
    public static bool PrefersPortuguese(IList<StringWithQualityHeaderValue> acceptLanguage) =>
        acceptLanguage
            .OrderByDescending(language => language.Quality ?? 1)
            .Select(language => language.Value.Value ?? string.Empty)
            .FirstOrDefault(language => language.StartsWith("pt", StringComparison.OrdinalIgnoreCase)
                || language.StartsWith("en", StringComparison.OrdinalIgnoreCase))
            ?.StartsWith("pt", StringComparison.OrdinalIgnoreCase) == true;
}
