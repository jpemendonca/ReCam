using System.Net;
using System.Net.Http.Json;
using System.Web;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Recam.Server.Domain;
using Recam.Server.Features.Pairing;
using Recam.Server.Features.Setup;
using Recam.Server.Infrastructure.Hosting;
using Recam.Server.Tests.Support;

namespace Recam.Server.Tests.Infrastructure.Http;

public sealed class ReverseProxyTests
{
    private static readonly Uri FirstOpenUri = new("/api/web/first-open", UriKind.Relative);

    [Fact(DisplayName = "With TLS off, camera QR codes carry no fingerprint either")]
    public async Task CreatePairingToken_WithTlsOff_HasNoFingerprint()
    {
        // arrange
        using var factory = new RecamApiFactory { Settings = new Dictionary<string, string> { [ServerSettings.TlsKey] = "off" } };
        var owner = await factory.PairDeviceAsync(DeviceRole.Owner);
        using var client = factory.CreateDeviceClient(owner.Credential);

        // act
        using var response = await client.PostAsJsonAsync(
            new Uri("/api/pairing-tokens", UriKind.Relative), new { role = "camera" }, ApiJson.Options, TestContext.Current.CancellationToken);

        // assert
        var body = await response.Content.ReadFromJsonAsync<CreatePairingTokenResponse>(ApiJson.Options, TestContext.Current.CancellationToken);
        Assert.DoesNotContain("&f=", body!.QrUri, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "With the server's own TLS, QR codes keep the fingerprint")]
    public async Task CreatePairingToken_WithTlsOn_HasFingerprint()
    {
        // arrange
        using var factory = new RecamApiFactory();
        var owner = await factory.PairDeviceAsync(DeviceRole.Owner);
        using var client = factory.CreateDeviceClient(owner.Credential);

        // act
        using var response = await client.PostAsJsonAsync(
            new Uri("/api/pairing-tokens", UriKind.Relative), new { role = "camera" }, ApiJson.Options, TestContext.Current.CancellationToken);

        // assert
        var body = await response.Content.ReadFromJsonAsync<CreatePairingTokenResponse>(ApiJson.Options, TestContext.Current.CancellationToken);
        var query = HttpUtility.ParseQueryString(new Uri(body!.QrUri).Query);
        Assert.Equal(64, query["f"]?.Length);
    }

    [Theory(DisplayName = "Behind a trusted proxy, the limit on first-time code tries counts each real client apart")]
    [InlineData("10.0.0.0/24", "10.0.0.5", HttpStatusCode.Unauthorized)]
    [InlineData("10.0.0.5", "10.0.0.9", HttpStatusCode.TooManyRequests)]
    public async Task FirstOpen_BehindProxy_LimitsTheRealClient(string trusted, string proxy, HttpStatusCode otherClient)
    {
        // arrange
        using var factory = new RecamApiFactory
        {
            RemoteIpAddress = IPAddress.Parse(proxy),
            Settings = new Dictionary<string, string> { [ServerSettings.TrustedProxiesKey] = trusted },
        };
        await factory.FirstOpenCodeAsync();
        using var http = factory.CreateBrowserClient();
        for (var attempt = 0; attempt < FirstOpenCode.MaxAttempts; attempt++)
        {
            using var guess = await WrongCodeFromAsync(http, "203.0.113.7");
        }

        // act
        using var response = await WrongCodeFromAsync(http, "198.51.100.4");

        // assert
        // A trusted proxy's header is believed, so the other client still has its tries; from a
        // proxy nobody named, everyone looks like the proxy and shares one limit.
        Assert.Equal(otherClient, response.StatusCode);
    }

    [Theory(DisplayName = "A value that is not an address or network stops the server at startup")]
    [InlineData("not-an-ip")]
    [InlineData("10.0.0.5, bogus/99")]
    public void From_WithBadTrustedProxy_Throws(string value)
    {
        // arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { [ServerSettings.TrustedProxiesKey] = value })
            .Build();

        // act
        var exception = Record.Exception(() => ServerSettings.From(configuration, AppContext.BaseDirectory));

        // assert
        Assert.IsType<InvalidOperationException>(exception);
    }

    private static async Task<HttpResponseMessage> WrongCodeFromAsync(HttpClient http, string client)
    {
        using var request = FirstOpenRequest("AAAA-AAAA");
        request.Headers.Add("X-Forwarded-For", client);
        return await http.SendAsync(request, TestContext.Current.CancellationToken);
    }

    private static HttpRequestMessage FirstOpenRequest(string code) => new(HttpMethod.Post, FirstOpenUri)
    {
        Content = JsonContent.Create(new { code, remember = false }, options: ApiJson.Options),
    };
}
