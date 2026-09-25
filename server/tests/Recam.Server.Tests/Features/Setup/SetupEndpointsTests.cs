using System.Net;
using Recam.Server.Domain;
using Recam.Server.Tests.Support;

namespace Recam.Server.Tests.Features.Setup;

public sealed class SetupEndpointsTests
{
    private static readonly Uri SetupUri = new("/setup", UriKind.Relative);

    [Fact(DisplayName = "Setup page shows the owner QR code to the local network")]
    public async Task SetupPage_FromLocalNetwork_ShowsQrCode()
    {
        // arrange
        using var factory = new RecamApiFactory { RemoteIpAddress = IPAddress.Parse("192.168.0.20") };
        using var client = factory.CreateClient();

        // act
        using var response = await client.GetAsync(SetupUri, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("<svg", body, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Setup page also shows the pairing code as text, for phones that cannot scan")]
    public async Task SetupPage_FromLocalNetwork_ShowsPairingUriAsText()
    {
        // arrange
        using var factory = new RecamApiFactory();
        using var client = factory.CreateClient();

        // act
        using var response = await client.GetAsync(SetupUri, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.Contains("recam://pair?v=1&amp;t=", body, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Once an owner is paired, the setup page no longer hands out a token")]
    public async Task SetupPage_WithOwner_ShowsAlreadyConfigured()
    {
        // arrange
        using var factory = new RecamApiFactory();
        await factory.PairDeviceAsync(DeviceRole.Owner);
        using var client = factory.CreateClient();

        // act
        using var response = await client.GetAsync(SetupUri, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("already configured", body, StringComparison.Ordinal);
        Assert.DoesNotContain("<svg", body, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Setup page refuses requests that came through a proxy")]
    public async Task SetupPage_FromForwardedRequest_Returns403()
    {
        // arrange
        using var factory = new RecamApiFactory();
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, SetupUri);
        request.Headers.Add("X-Forwarded-For", "203.0.113.7");

        // act
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact(DisplayName = "Setup page refuses requests from a public address")]
    public async Task SetupPage_FromPublicAddress_Returns403()
    {
        // arrange
        using var factory = new RecamApiFactory { RemoteIpAddress = IPAddress.Parse("203.0.113.7") };
        using var client = factory.CreateClient();

        // act
        using var response = await client.GetAsync(SetupUri, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
