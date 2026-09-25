using System.Net;
using Microsoft.AspNetCore.SignalR.Client;
using Recam.Server.Domain;
using Recam.Server.Features.Setup;
using Recam.Server.Infrastructure.Realtime;
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

    [Fact(DisplayName = "Setup page tells the person to choose Watch, and the phone becomes a Monitor")]
    public async Task SetupPage_WithoutOwner_SpeaksOfWatchAndMonitor()
    {
        // arrange
        using var factory = new RecamApiFactory();
        using var client = factory.CreateClient();

        // act
        var body = await client.GetStringAsync(SetupUri, TestContext.Current.CancellationToken);

        // assert
        Assert.Contains("<strong>Assistir</strong>", body, StringComparison.Ordinal);
        Assert.Contains("<strong>Watch</strong>", body, StringComparison.Ordinal);
        Assert.Contains("Monitor", body, StringComparison.Ordinal);
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

    [Fact(DisplayName = "Once a phone owns the server, the page lists the devices instead of handing out a token")]
    public async Task SetupPage_WithOwner_ListsDevices()
    {
        // arrange
        using var factory = new RecamApiFactory();
        var owner = await factory.PairDeviceAsync(DeviceRole.Owner, "Pedro's phone");
        var camera = await factory.PairDeviceAsync(DeviceRole.Camera, "Porch");
        using var client = factory.CreateClient();

        // act
        using var response = await client.GetAsync(SetupUri, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("<svg", body, StringComparison.Ordinal);
        Assert.DoesNotContain("recam://pair", body, StringComparison.Ordinal);
        Assert.Contains("Pedro&#39;s phone", Row(body, owner.DeviceId), StringComparison.Ordinal);
        Assert.Contains("<td>Monitor</td>", Row(body, owner.DeviceId), StringComparison.Ordinal);
        Assert.Contains("Camera · Câmera", Row(body, camera.DeviceId), StringComparison.Ordinal);
        Assert.Contains(
            $"<meta http-equiv=\"refresh\" content=\"{SetupPage.PanelRefreshSeconds}\">", body, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "The panel shows which camera is online, streaming and how many watch it")]
    public async Task SetupPage_WithCameraStreaming_ShowsPresenceAndTransmission()
    {
        // arrange
        using var factory = new RecamApiFactory();
        var owner = await factory.PairDeviceAsync(DeviceRole.Owner);
        var porch = await factory.PairDeviceAsync(DeviceRole.Camera, "Porch");
        var garage = await factory.PairDeviceAsync(DeviceRole.Camera, "Garage");
        await using var cameraConnection = await factory.ConnectAsync(porch.Credential);
        await cameraConnection.InvokeAsync<HubResult>("ReportTelemetry", 64, true, TestContext.Current.CancellationToken);
        await cameraConnection.InvokeAsync<HubResult>("ReportPublishing", true, TestContext.Current.CancellationToken);
        await using var viewerConnection = await factory.ConnectAsync(owner.Credential);
        await viewerConnection.InvokeAsync<HubResult>("WatchCamera", porch.DeviceId, TestContext.Current.CancellationToken);
        using var client = factory.CreateClient();

        // act
        var body = await client.GetStringAsync(SetupUri, TestContext.Current.CancellationToken);

        // assert
        var porchRow = Row(body, porch.DeviceId);
        Assert.Equal(
            "<td>Porch</td><td>Camera · Câmera</td><td><span class=\"on\">yes · sim</span></td><td>yes · sim</td><td>1</td><td>64% ⚡</td>",
            porchRow);
        var garageRow = Row(body, garage.DeviceId);
        Assert.Equal(
            "<td>Garage</td><td>Camera · Câmera</td><td><span class=\"off\">no · não</span></td><td>no · não</td><td>0</td><td>—</td>",
            garageRow);
        Assert.Contains("<span class=\"on\">", Row(body, owner.DeviceId), StringComparison.Ordinal);
    }

    [Fact(DisplayName = "The panel keeps the local-network rule once a phone owns the server")]
    public async Task SetupPage_WithOwner_FromPublicAddress_Returns403()
    {
        // arrange
        using var factory = new RecamApiFactory();
        await factory.PairDeviceAsync(DeviceRole.Owner);
        factory.RemoteIpAddress = IPAddress.Parse("203.0.113.7");
        using var client = factory.CreateClient();

        // act
        using var response = await client.GetAsync(SetupUri, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
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

    /// <summary>The cells of one device's row in the panel.</summary>
    private static string Row(string body, Guid deviceId)
    {
        var start = $"<tr data-device=\"{deviceId:N}\">";
        var from = body.IndexOf(start, StringComparison.Ordinal);
        Assert.True(from >= 0, $"No row for device {deviceId}.");
        from += start.Length;
        return body[from..body.IndexOf("</tr>", from, StringComparison.Ordinal)];
    }
}
