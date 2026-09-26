using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Recam.Server.Domain;
using Recam.Server.Features.Devices;
using Recam.Server.Features.Setup;
using Recam.Server.Tests.Support;

namespace Recam.Server.Tests.Features.Setup;

public sealed class FirstOpenEndpointsTests
{
    private const string ChromeOnWindows =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/140.0.0.0 Safari/537.36";

    private static readonly Uri FirstOpenUri = new("/api/web/first-open", UriKind.Relative);
    private static readonly Uri SignOutUri = new("/api/web/sign-out", UriKind.Relative);
    private static readonly Uri MeUri = new("/api/me", UriKind.Relative);

    [Fact(DisplayName = "The right code from the local network makes the browser the owner and sets the cookie")]
    public async Task Open_RightCode_CreatesOwnerAndCookie()
    {
        // arrange
        using var factory = new RecamApiFactory { RemoteIpAddress = IPAddress.Parse("192.168.0.20") };
        var code = await factory.FirstOpenCodeAsync();
        using var client = factory.CreateBrowserClient();

        // act
        using var response = await OpenAsync(client, code, remember: false);

        // assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var cookie = Assert.Single(response.Headers.GetValues("Set-Cookie"));
        Assert.StartsWith("recam_device=", cookie, StringComparison.Ordinal);
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("expires", cookie, StringComparison.OrdinalIgnoreCase);
        await using var database = await factory.CreateDatabaseAsync();
        var owner = await database.Devices.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(DeviceRole.Owner, owner.Role);
        Assert.Equal("Navegador · Chrome no Windows", owner.Name);
    }

    [Fact(DisplayName = "With Remember on this computer, the cookie lasts 180 days")]
    public async Task Open_WithRemember_CookieExpiresLater()
    {
        // arrange
        using var factory = new RecamApiFactory();
        var code = await factory.FirstOpenCodeAsync();
        using var client = factory.CreateBrowserClient();

        // act
        using var response = await OpenAsync(client, code, remember: true);

        // assert
        var cookie = Assert.Single(response.Headers.GetValues("Set-Cookie"));
        var expires = factory.Time.GetUtcNow().AddDays(180).ToString("R", System.Globalization.CultureInfo.InvariantCulture);
        Assert.Contains($"expires={expires}", cookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact(DisplayName = "The cookie authenticates the browser, and the log never carries its credential")]
    public async Task Me_WithCookie_ReturnsOwner()
    {
        // arrange
        using var factory = new RecamApiFactory();
        using var client = factory.CreateBrowserClient();
        var cookie = await OpenAndGetCookieAsync(factory, client);
        using var request = new HttpRequestMessage(HttpMethod.Get, MeUri);
        request.Headers.Add("Cookie", cookie);

        // act
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        var me = await response.Content.ReadFromJsonAsync<MeResponse>(ApiJson.Options, TestContext.Current.CancellationToken);
        Assert.Equal(DeviceRole.Owner, me!.Role);
        var credential = cookie["recam_device=".Length..];
        Assert.DoesNotContain(factory.Logs.Messages, message => message.Contains(credential, StringComparison.Ordinal));
        Assert.DoesNotContain(factory.Logs.Messages, message => message.Contains(credential.Split('.')[1], StringComparison.Ordinal));
    }

    [Fact(DisplayName = "The status says whether a browser may still become the first Monitor")]
    public async Task Status_BeforeAndAfterOpening_ChangesToClosed()
    {
        // arrange
        using var factory = new RecamApiFactory();
        using var client = factory.CreateBrowserClient();
        var before = await client.GetFromJsonAsync<FirstOpenStatusResponse>(FirstOpenUri, ApiJson.Options, TestContext.Current.CancellationToken);

        // act
        await OpenAndGetCookieAsync(factory, client);

        // assert
        var after = await client.GetFromJsonAsync<FirstOpenStatusResponse>(FirstOpenUri, ApiJson.Options, TestContext.Current.CancellationToken);
        Assert.True(before!.Open);
        Assert.False(after!.Open);
    }

    [Fact(DisplayName = "A wrong code is refused, and after five a new code replaces the old one")]
    public async Task Open_FiveWrongCodes_ReplacesCode()
    {
        // arrange
        using var factory = new RecamApiFactory();
        var first = await factory.FirstOpenCodeAsync();
        using var client = factory.CreateBrowserClient();
        var statuses = new List<HttpStatusCode>();
        for (var attempt = 0; attempt < FirstOpenCode.MaxAttempts; attempt++)
        {
            using var wrong = await OpenAsync(client, "AAAA-AAAA", remember: false);
            statuses.Add(wrong.StatusCode);
        }

        // act
        using var withOldCode = await OpenLimitFreeAsync(factory, first);

        // assert
        Assert.All(statuses, status => Assert.Equal(HttpStatusCode.Unauthorized, status));
        Assert.Equal(HttpStatusCode.Unauthorized, withOldCode.StatusCode);
        var codes = factory.FirstOpenCodesInLog().ToList();
        Assert.Equal(2, codes.Count);
        Assert.NotEqual(codes[0], codes[1]);
    }

    [Fact(DisplayName = "With a Monitor already on the server, the code opens nothing")]
    public async Task Open_WithMonitor_ReturnsConflict()
    {
        // arrange
        using var factory = new RecamApiFactory();
        var code = await factory.FirstOpenCodeAsync();
        await factory.PairDeviceAsync(DeviceRole.Owner);
        using var client = factory.CreateBrowserClient();

        // act
        using var response = await OpenAsync(client, code, remember: false);

        // assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("setup.already_has_monitor", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), StringComparison.Ordinal);
    }

    [Fact(DisplayName = "From outside the local network, as on a VPS, the right code also makes the browser the owner")]
    public async Task Open_FromPublicAddress_MakesOwner()
    {
        // arrange
        using var factory = new RecamApiFactory { RemoteIpAddress = IPAddress.Parse("203.0.113.7") };
        var code = await factory.FirstOpenCodeAsync();
        using var client = factory.CreateBrowserClient();

        // act
        using var response = await OpenAsync(client, code, remember: false);

        // assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact(DisplayName = "From outside, guessing is still held back: five tries a minute per address")]
    public async Task Open_FromPublicAddress_KeepsRateLimit()
    {
        // arrange
        using var factory = new RecamApiFactory { RemoteIpAddress = IPAddress.Parse("203.0.113.7") };
        await factory.FirstOpenCodeAsync();
        using var client = factory.CreateBrowserClient();
        for (var attempt = 0; attempt < FirstOpenCode.MaxAttempts; attempt++)
        {
            using var wrong = await OpenAsync(client, "AAAA-AAAA", remember: false);
        }

        // act
        using var response = await OpenAsync(client, "AAAA-AAAA", remember: false);

        // assert
        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
    }

    [Fact(DisplayName = "A cookie request that changes something needs the X-Recam-Web header")]
    public async Task SignOut_CookieWithoutWebHeader_IsRefused()
    {
        // arrange
        using var factory = new RecamApiFactory();
        using var client = factory.CreateBrowserClient();
        var cookie = await OpenAndGetCookieAsync(factory, client);
        using var request = new HttpRequestMessage(HttpMethod.Post, SignOutUri);
        request.Headers.Add("Cookie", cookie);

        // act
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        await using var database = await factory.CreateDatabaseAsync();
        Assert.False((await database.Devices.SingleAsync(TestContext.Current.CancellationToken)).IsRevoked);
    }

    [Fact(DisplayName = "Signing out revokes the browser on the server and deletes the cookie")]
    public async Task SignOut_WithWebHeader_RevokesAndDeletesCookie()
    {
        // arrange
        using var factory = new RecamApiFactory();
        using var client = factory.CreateBrowserClient();
        var cookie = await OpenAndGetCookieAsync(factory, client);
        using var request = new HttpRequestMessage(HttpMethod.Post, SignOutUri);
        request.Headers.Add("Cookie", cookie);
        request.Headers.Add("X-Recam-Web", "1");

        // act
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Contains("expires=Thu, 01 Jan 1970", Assert.Single(response.Headers.GetValues("Set-Cookie")), StringComparison.OrdinalIgnoreCase);
        using var me = new HttpRequestMessage(HttpMethod.Get, MeUri);
        me.Headers.Add("Cookie", cookie);
        using var meResponse = await client.SendAsync(me, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, meResponse.StatusCode);
        var status = await client.GetFromJsonAsync<FirstOpenStatusResponse>(FirstOpenUri, ApiJson.Options, TestContext.Current.CancellationToken);
        Assert.True(status!.Open);
    }

    [Fact(DisplayName = "The old /setup address sends people to the browser Monitor")]
    public async Task Setup_Redirects_ToRoot()
    {
        // arrange
        using var factory = new RecamApiFactory();
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false });

        // act
        using var response = await client.GetAsync(new Uri("/setup", UriKind.Relative), TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/", response.Headers.Location?.OriginalString);
    }

    private static HttpRequestMessage OpenRequest(string code, bool remember)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, FirstOpenUri)
        {
            Content = JsonContent.Create(new { code, remember }, options: ApiJson.Options),
        };
        request.Headers.Add("User-Agent", ChromeOnWindows);
        request.Headers.Add("Accept-Language", "pt-BR,pt;q=0.9");
        return request;
    }

    private static async Task<HttpResponseMessage> OpenAsync(HttpClient client, string code, bool remember)
    {
        using var request = OpenRequest(code, remember);
        return await client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    // The five wrong tries used this IP's rate limit; a different IP reaches the endpoint.
    private static async Task<HttpResponseMessage> OpenLimitFreeAsync(RecamApiFactory factory, string code)
    {
        factory.RemoteIpAddress = IPAddress.Parse("192.168.0.99");
        using var client = factory.CreateBrowserClient();
        return await OpenAsync(client, code, remember: false);
    }

    private static async Task<string> OpenAndGetCookieAsync(RecamApiFactory factory, HttpClient client)
    {
        using var response = await OpenAsync(client, await factory.FirstOpenCodeAsync(), remember: false);
        response.EnsureSuccessStatusCode();
        return Assert.Single(response.Headers.GetValues("Set-Cookie")).Split(';')[0];
    }
}
