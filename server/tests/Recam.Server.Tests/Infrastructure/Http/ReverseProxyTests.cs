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
    private static readonly Uri SetupUri = new("/setup", UriKind.Relative);
    private static readonly IPAddress Proxy = IPAddress.Parse("10.0.0.5");

    [Fact(DisplayName = "With TLS off behind a proxy, the first-Monitor QR carries no fingerprint")]
    public async Task OwnerQr_WithTlsOff_HasNoFingerprint()
    {
        // arrange
        using var factory = new RecamApiFactory
        {
            Settings = new Dictionary<string, string>
            {
                [ServerSettings.TlsKey] = "off",
                [ServerSettings.PublicUrlsKey] = "https://recam.example.com",
            },
        };
        var ownerSetup = factory.Services.GetRequiredService<OwnerSetup>();

        // act
        var status = await ownerSetup.EnsureTokenAsync(TestContext.Current.CancellationToken);

        // assert
        var query = HttpUtility.ParseQueryString(new Uri(Assert.IsType<OwnerSetupStatus.Pending>(status).PairingUri).Query);
        Assert.Null(query["f"]);
        Assert.Equal("https://recam.example.com", query["u"]);
    }

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
    public async Task OwnerQr_WithTlsOn_HasFingerprint()
    {
        // arrange
        using var factory = new RecamApiFactory();
        var ownerSetup = factory.Services.GetRequiredService<OwnerSetup>();

        // act
        var status = await ownerSetup.EnsureTokenAsync(TestContext.Current.CancellationToken);

        // assert
        var query = HttpUtility.ParseQueryString(new Uri(Assert.IsType<OwnerSetupStatus.Pending>(status).PairingUri).Query);
        Assert.Equal(64, query["f"]?.Length);
    }

    [Theory(DisplayName = "Behind a trusted proxy, the setup page sees the real client address")]
    [InlineData("192.168.0.20", HttpStatusCode.OK)]
    [InlineData("203.0.113.7", HttpStatusCode.Forbidden)]
    public async Task SetupPage_ThroughTrustedProxy_UsesForwardedClient(string client, HttpStatusCode expected)
    {
        // arrange
        using var factory = new RecamApiFactory
        {
            RemoteIpAddress = Proxy,
            Settings = new Dictionary<string, string> { [ServerSettings.TrustedProxiesKey] = "10.0.0.0/24" },
        };
        using var http = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, SetupUri);
        request.Headers.Add("X-Forwarded-For", client);

        // act
        using var response = await http.SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(expected, response.StatusCode);
    }

    [Fact(DisplayName = "Forwarded headers from a proxy nobody named are not believed")]
    public async Task SetupPage_ThroughUntrustedProxy_IsRefused()
    {
        // arrange
        using var factory = new RecamApiFactory
        {
            RemoteIpAddress = IPAddress.Parse("10.0.0.9"),
            Settings = new Dictionary<string, string> { [ServerSettings.TrustedProxiesKey] = "10.0.0.5" },
        };
        using var http = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, SetupUri);
        request.Headers.Add("X-Forwarded-For", "192.168.0.20");

        // act
        using var response = await http.SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
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
        var exception = Record.Exception(() => ServerSettings.From(configuration, "/"));

        // assert
        Assert.IsType<InvalidOperationException>(exception);
    }
}
