using Microsoft.Net.Http.Headers;
using Recam.Server.Infrastructure.Http;

namespace Recam.Server.Tests.Infrastructure.Http;

public sealed class BrowserDeviceNameTests
{
    private const string ChromeOnWindows =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/140.0.0.0 Safari/537.36";

    private const string EdgeOnWindows = ChromeOnWindows + " Edg/140.0.0.0";

    private const string FirefoxOnLinux = "Mozilla/5.0 (X11; Ubuntu; Linux x86_64; rv:143.0) Gecko/20100101 Firefox/143.0";

    private const string SafariOnIphone =
        "Mozilla/5.0 (iPhone; CPU iPhone OS 18_6 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.6 Mobile/15E148 Safari/604.1";

    [Theory(DisplayName = "The browser gets a name with its browser and system, in the browser's language")]
    [InlineData(ChromeOnWindows, "pt-BR,pt;q=0.9", "Navegador · Chrome no Windows")]
    [InlineData(EdgeOnWindows, "en-US,en;q=0.9", "Browser · Edge on Windows")]
    [InlineData(FirefoxOnLinux, "pt", "Navegador · Firefox no Linux")]
    [InlineData(SafariOnIphone, "de-DE,en;q=0.5", "Browser · Safari on iOS")]
    [InlineData("curl/8.0", "pt-BR", "Navegador")]
    [InlineData(null, "", "Browser")]
    public void From_UserAgent_NamesBrowserAndSystem(string? userAgent, string acceptLanguage, string expected)
    {
        // arrange
        var languages = StringWithQualityHeaderValue.ParseList(acceptLanguage.Split(',', StringSplitOptions.RemoveEmptyEntries));

        // act
        var name = BrowserDeviceName.From(userAgent, languages);

        // assert
        Assert.Equal(expected, name);
    }
}
