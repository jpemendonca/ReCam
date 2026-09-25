using System.Net;
using Microsoft.AspNetCore.SignalR.Client;
using Recam.Server.Domain;
using Recam.Server.Features.Realtime;
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

    [Fact(DisplayName = "In Portuguese, the page walks through pairing the first Monitor with Assistir")]
    public async Task SetupPage_WithoutOwner_InPortuguese_ShowsSteps()
    {
        // arrange
        using var factory = new RecamApiFactory();
        using var client = factory.CreateClient();

        // act
        var body = await GetPageAsync(client, "pt-BR,pt;q=0.9,en;q=0.8");

        // assert
        Assert.Contains("<html lang=\"pt-BR\">", body, StringComparison.Ordinal);
        Assert.Contains("<li>Na primeira tela, toque em <strong>Assistir</strong>.</li>", body, StringComparison.Ordinal);
        Assert.Contains("Monitor", body, StringComparison.Ordinal);
        Assert.DoesNotContain("<strong>Watch</strong>", body, StringComparison.Ordinal);
        Assert.Contains("<svg", body, StringComparison.Ordinal);
        AssertNoScript(body);
    }

    [Fact(DisplayName = "In English, the page walks through pairing the first Monitor with Watch")]
    public async Task SetupPage_WithoutOwner_InEnglish_ShowsSteps()
    {
        // arrange
        using var factory = new RecamApiFactory();
        using var client = factory.CreateClient();

        // act
        var body = await GetPageAsync(client, "en-US,en;q=0.9");

        // assert
        Assert.Contains("<html lang=\"en\">", body, StringComparison.Ordinal);
        Assert.Contains("<li>On the first screen, tap <strong>Watch</strong>.</li>", body, StringComparison.Ordinal);
        Assert.DoesNotContain("Assistir", body, StringComparison.Ordinal);
        AssertNoScript(body);
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

    [Fact(DisplayName = "Once a Monitor is paired, the page shows camera and Monitor cards instead of a token")]
    public async Task SetupPage_WithOwner_InEnglish_ShowsCards()
    {
        // arrange
        using var factory = new RecamApiFactory();
        var owner = await factory.PairDeviceAsync(DeviceRole.Owner, "Pedro's phone");
        var camera = await factory.PairDeviceAsync(DeviceRole.Camera, "Porch");
        using var client = factory.CreateClient();

        // act
        var body = await GetPageAsync(client, "en");

        // assert
        Assert.DoesNotContain("<svg", body, StringComparison.Ordinal);
        Assert.DoesNotContain("recam://pair", body, StringComparison.Ordinal);
        Assert.Contains("<h2>Cameras <span class=\"count\">1</span></h2>", body, StringComparison.Ordinal);
        Assert.Contains("<h2>Monitors <span class=\"count\">1</span></h2>", body, StringComparison.Ordinal);
        Assert.Contains("<h3>Pedro&#x27;s phone</h3>", Card(body, owner.DeviceId), StringComparison.Ordinal);
        Assert.Contains("<h3>Porch</h3>", Card(body, camera.DeviceId), StringComparison.Ordinal);
        Assert.Contains("<h2>Add a camera</h2>", body, StringComparison.Ordinal);
        Assert.Contains(
            $"<meta http-equiv=\"refresh\" content=\"{SetupPage.PanelRefreshSeconds}\">", body, StringComparison.Ordinal);
        AssertNoScript(body);
    }

    [Fact(DisplayName = "In Portuguese, the panel speaks of Câmeras and Monitores")]
    public async Task SetupPage_WithOwner_InPortuguese_ShowsCards()
    {
        // arrange
        using var factory = new RecamApiFactory();
        await factory.PairDeviceAsync(DeviceRole.Owner);
        using var client = factory.CreateClient();

        // act
        var body = await GetPageAsync(client, "pt-BR");

        // assert
        Assert.Contains("<h2>Câmeras <span class=\"count\">0</span></h2>", body, StringComparison.Ordinal);
        Assert.Contains("<h2>Monitores <span class=\"count\">1</span></h2>", body, StringComparison.Ordinal);
        Assert.Contains("Nenhuma câmera ainda.", body, StringComparison.Ordinal);
        Assert.Contains("toque em <strong>Filmar</strong>", body, StringComparison.Ordinal);
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
        await cameraConnection.InvokeAsync<HubResult>("ReportTelemetry", new TelemetryReport(64, true, null), TestContext.Current.CancellationToken);
        await cameraConnection.InvokeAsync<HubResult>("ReportPublishing", true, TestContext.Current.CancellationToken);
        await using var viewerConnection = await factory.ConnectAsync(owner.Credential);
        await viewerConnection.InvokeAsync<HubResult>("WatchCamera", porch.DeviceId, TestContext.Current.CancellationToken);
        using var client = factory.CreateClient();

        // act
        var body = await GetPageAsync(client, "pt-BR");

        // assert
        var porchCard = Card(body, porch.DeviceId);
        Assert.Contains("<span class=\"badge on\">Online</span> <span class=\"badge live\">Transmitindo</span>", porchCard, StringComparison.Ordinal);
        Assert.Contains("<dt>Assistindo agora</dt><dd>1</dd>", porchCard, StringComparison.Ordinal);
        Assert.Contains("<dt>Bateria</dt><dd>64% (carregando)</dd>", porchCard, StringComparison.Ordinal);
        var garageCard = Card(body, garage.DeviceId);
        Assert.Contains("<span class=\"badge off\">Offline</span></p>", garageCard, StringComparison.Ordinal);
        Assert.Contains("<dt>Bateria</dt><dd>—</dd>", garageCard, StringComparison.Ordinal);
        Assert.Contains("<span class=\"badge on\">Online</span>", Card(body, owner.DeviceId), StringComparison.Ordinal);
    }

    [Fact(DisplayName = "When the last Monitor leaves, the page shows the first-phone QR again")]
    public async Task SetupPage_AfterLastMonitorLeaves_ShowsQrAgain()
    {
        // arrange
        using var factory = new RecamApiFactory();
        var owner = await factory.PairDeviceAsync(DeviceRole.Owner);
        await factory.PairDeviceAsync(DeviceRole.Camera, "Porch");
        using var client = factory.CreateClient();
        var withMonitor = await client.GetStringAsync(SetupUri, TestContext.Current.CancellationToken);
        using var ownerClient = factory.CreateDeviceClient(owner.Credential);
        using var left = await ownerClient.DeleteAsync(new Uri("/api/me", UriKind.Relative), TestContext.Current.CancellationToken);

        // act
        var body = await client.GetStringAsync(SetupUri, TestContext.Current.CancellationToken);

        // assert
        Assert.DoesNotContain("<svg", withMonitor, StringComparison.Ordinal);
        Assert.Contains("<svg", body, StringComparison.Ordinal);
        Assert.Contains("recam://pair?v=1&amp;t=", body, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "While a viewer Monitor remains, the owner leaving keeps the panel")]
    public async Task SetupPage_WhenOwnerLeavesButViewerStays_KeepsPanel()
    {
        // arrange
        using var factory = new RecamApiFactory();
        var owner = await factory.PairDeviceAsync(DeviceRole.Owner);
        await factory.PairDeviceAsync(DeviceRole.Viewer, "Second Monitor");
        using var ownerClient = factory.CreateDeviceClient(owner.Credential);
        using var left = await ownerClient.DeleteAsync(new Uri("/api/me", UriKind.Relative), TestContext.Current.CancellationToken);
        using var client = factory.CreateClient();

        // act
        var body = await client.GetStringAsync(SetupUri, TestContext.Current.CancellationToken);

        // assert
        Assert.DoesNotContain("<svg", body, StringComparison.Ordinal);
        Assert.Contains("Second Monitor", body, StringComparison.Ordinal);
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

    private static async Task<string> GetPageAsync(HttpClient client, string acceptLanguage)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, SetupUri);
        request.Headers.Add("Accept-Language", acceptLanguage);
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
    }

    private static void AssertNoScript(string body)
    {
        Assert.DoesNotContain("<script", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(" onclick=", body, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>The inside of one device's card in the panel.</summary>
    private static string Card(string body, Guid deviceId)
    {
        var start = $"data-device=\"{deviceId:N}\">";
        var from = body.IndexOf(start, StringComparison.Ordinal);
        Assert.True(from >= 0, $"No card for device {deviceId}.");
        from += start.Length;
        return body[from..body.IndexOf("</article>", from, StringComparison.Ordinal)];
    }
}
