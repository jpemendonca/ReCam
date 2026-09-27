using System.Net;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Recam.Server.Domain;
using Recam.Server.Infrastructure.Realtime;
using Recam.Server.Tests.Support;

namespace Recam.Server.Tests.Features.Web;

/// <summary>The browser Monitor reaches the hub and WHEP with its cookie, like the app does with its bearer.</summary>
public sealed class BrowserMonitorTests
{
    [Fact(DisplayName = "With its cookie and the web header, the browser opens the hub and watches a camera")]
    public async Task Hub_WithCookie_WatchesCamera()
    {
        // arrange
        using var factory = new RecamApiFactory();
        var cookie = await factory.OpenBrowserMonitorAsync();
        var camera = await factory.PairDeviceAsync(DeviceRole.Camera);
        await using var connection = BrowserHub(factory, cookie, webHeader: true);
        await connection.StartAsync(TestContext.Current.CancellationToken);

        // act
        var result = await connection.InvokeAsync<HubResult>("WatchCamera", camera.DeviceId, TestContext.Current.CancellationToken);

        // assert
        Assert.True(result.Ok);
    }

    [Fact(DisplayName = "Without the web header, the cookie does not open the hub")]
    public async Task Hub_CookieWithoutWebHeader_IsRefused()
    {
        // arrange
        using var factory = new RecamApiFactory();
        var cookie = await factory.OpenBrowserMonitorAsync();
        await using var connection = BrowserHub(factory, cookie, webHeader: false);

        // act
        var exception = await Record.ExceptionAsync(() => connection.StartAsync(TestContext.Current.CancellationToken));

        // assert
        Assert.Equal(HttpStatusCode.Unauthorized, Assert.IsType<HttpRequestException>(exception).StatusCode);
    }

    [Fact(DisplayName = "WHEP accepts the cookie and never passes it on to MediaMTX")]
    public async Task Whep_WithCookie_ProxiesWithoutCookie()
    {
        // arrange
        using var mediaMtx = new FakeMediaMtx();
        using var factory = new RecamApiFactory { MediaMtxUrl = mediaMtx.Url };
        var cookie = await factory.OpenBrowserMonitorAsync();
        var camera = await factory.PairDeviceAsync(DeviceRole.Camera);
        await factory.SetRecordingAsync(camera.DeviceId, recording: false);
        using var client = factory.CreateBrowserClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri($"/whep/{camera.DeviceId}", UriKind.Relative))
        {
            Content = new StringContent("v=0\r\n", System.Text.Encoding.UTF8, "application/sdp"),
        };
        request.Headers.Add("Cookie", cookie);
        request.Headers.Add("X-Recam-Web", "1");

        // act
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal($"POST /cam-{camera.DeviceId:N}/whep", Assert.Single(mediaMtx.Requests));
        Assert.DoesNotContain("Cookie", mediaMtx.HeaderNames, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("X-Recam-Web", mediaMtx.HeaderNames, StringComparer.OrdinalIgnoreCase);
    }

    private static HubConnection BrowserHub(RecamApiFactory factory, string cookie, bool webHeader) =>
        new HubConnectionBuilder()
            .WithUrl(new Uri(new Uri("https://localhost"), "/hubs/devices"), options =>
            {
                options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
                options.Headers["Cookie"] = cookie;
                if (webHeader)
                {
                    options.Headers["X-Recam-Web"] = "1";
                }
            })
            .WithCamelCaseEnums()
            .Build();
}
