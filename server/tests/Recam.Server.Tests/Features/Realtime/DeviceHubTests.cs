using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Recam.Server.Domain;
using Recam.Server.Infrastructure.Realtime;
using Recam.Server.Tests.Support;

namespace Recam.Server.Tests.Features.Realtime;

public sealed class DeviceHubTests
{
    private static readonly TimeSpan Wait = TimeSpan.FromSeconds(10);
    private static readonly Uri CamerasUri = new("/api/cameras", UriKind.Relative);

    [Fact(DisplayName = "Telemetry from a camera reaches connected viewers and is stored")]
    public async Task ReportTelemetry_FromCamera_NotifiesViewers()
    {
        // arrange
        using var factory = new RecamApiFactory();
        var owner = await factory.PairDeviceAsync(DeviceRole.Owner);
        var camera = await factory.PairDeviceAsync(DeviceRole.Camera, "Porch");
        await using var viewerConnection = await factory.ConnectAsync(owner.Credential);
        using var updates = new StatusInbox(viewerConnection);
        await using var cameraConnection = await factory.ConnectAsync(camera.Credential);
        await updates.WaitForAsync(status => status.Online);

        // act
        var result = await cameraConnection.InvokeAsync<HubResult>(
            "ReportTelemetry", 42, true, TestContext.Current.CancellationToken);

        // assert
        Assert.True(result.Ok);
        var status = await updates.WaitForAsync(status => status.BatteryLevel == 42);
        Assert.Equal(camera.DeviceId, status.Id);
        Assert.True(status.IsCharging);
        using var client = factory.CreateDeviceClient(owner.Credential);
        var cameras = await client.GetFromJsonAsync<List<CameraStatus>>(CamerasUri, ApiJson.Options, TestContext.Current.CancellationToken);
        Assert.Equal(42, Assert.Single(cameras!).BatteryLevel);
    }

    [Fact(DisplayName = "Any device can check its connection with a heartbeat")]
    public async Task Heartbeat_FromAnyDevice_ReturnsOk()
    {
        // arrange
        using var factory = new RecamApiFactory();
        var camera = await factory.PairDeviceAsync(DeviceRole.Camera);
        await using var connection = await factory.ConnectAsync(camera.Credential);

        // act
        var result = await connection.InvokeAsync<HubResult>("Heartbeat", TestContext.Current.CancellationToken);

        // assert
        Assert.True(result.Ok);
    }

    [Fact(DisplayName = "An out of range battery level is refused as data, not as an exception")]
    public async Task ReportTelemetry_WithInvalidLevel_ReturnsFailure()
    {
        // arrange
        using var factory = new RecamApiFactory();
        var camera = await factory.PairDeviceAsync(DeviceRole.Camera);
        await using var connection = await factory.ConnectAsync(camera.Credential);

        // act
        var result = await connection.InvokeAsync<HubResult>(
            "ReportTelemetry", 101, false, TestContext.Current.CancellationToken);

        // assert
        Assert.False(result.Ok);
        Assert.Equal(DeviceErrors.InvalidBatteryLevel.Code, result.Code);
    }

    [Fact(DisplayName = "Only cameras may report telemetry")]
    public async Task ReportTelemetry_FromViewer_IsRejected()
    {
        // arrange
        using var factory = new RecamApiFactory();
        var owner = await factory.PairDeviceAsync(DeviceRole.Owner);
        await using var connection = await factory.ConnectAsync(owner.Credential);

        // act
        var exception = await Record.ExceptionAsync(() => connection.InvokeAsync<HubResult>(
            "ReportTelemetry", 50, false, TestContext.Current.CancellationToken));

        // assert
        Assert.IsType<HubException>(exception);
    }

    [Fact(DisplayName = "A camera that disconnects shows as offline to viewers")]
    public async Task Cameras_AfterCameraDisconnects_ShowsOffline()
    {
        // arrange
        using var factory = new RecamApiFactory();
        var owner = await factory.PairDeviceAsync(DeviceRole.Owner);
        var camera = await factory.PairDeviceAsync(DeviceRole.Camera);
        await using var viewerConnection = await factory.ConnectAsync(owner.Credential);
        using var updates = new StatusInbox(viewerConnection);
        var cameraConnection = await factory.ConnectAsync(camera.Credential);
        await updates.WaitForAsync(status => status.Online);

        // act
        await cameraConnection.DisposeAsync();

        // assert
        await updates.WaitForAsync(status => !status.Online);
        using var client = factory.CreateDeviceClient(owner.Credential);
        var cameras = await client.GetFromJsonAsync<List<CameraStatus>>(CamerasUri, ApiJson.Options, TestContext.Current.CancellationToken);
        Assert.False(Assert.Single(cameras!).Online);
    }

    [Fact(DisplayName = "A camera cannot list cameras")]
    public async Task Cameras_AsCamera_Returns403()
    {
        // arrange
        using var factory = new RecamApiFactory();
        var camera = await factory.PairDeviceAsync(DeviceRole.Camera);
        using var client = factory.CreateDeviceClient(camera.Credential);

        // act
        using var response = await client.GetAsync(CamerasUri, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact(DisplayName = "The hub accepts the credential in the access_token query value")]
    public async Task Negotiate_WithQueryToken_IsAccepted()
    {
        // arrange
        using var factory = new RecamApiFactory();
        var camera = await factory.PairDeviceAsync(DeviceRole.Camera);
        using var client = factory.CreateClient();
        var uri = new Uri(
            $"/hubs/devices/negotiate?negotiateVersion=1&access_token={Uri.EscapeDataString(camera.Credential)}",
            UriKind.Relative);

        // act
        using var response = await client.PostAsync(uri, null, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact(DisplayName = "The access_token query value is ignored outside the hub")]
    public async Task Me_WithQueryToken_Returns401()
    {
        // arrange
        using var factory = new RecamApiFactory();
        var camera = await factory.PairDeviceAsync(DeviceRole.Camera);
        using var client = factory.CreateClient();
        var uri = new Uri($"/api/me?access_token={Uri.EscapeDataString(camera.Credential)}", UriKind.Relative);

        // act
        using var response = await client.GetAsync(uri, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>Collects CameraStatusChanged messages and waits for one that matches.</summary>
    private sealed class StatusInbox : IDisposable
    {
        private readonly List<CameraStatus> _received = [];
        private readonly SemaphoreSlim _signal = new(0);

        public StatusInbox(HubConnection connection) =>
            connection.On<CameraStatus>("CameraStatusChanged", status =>
            {
                lock (_received)
                {
                    _received.Add(status);
                }

                _signal.Release();
            });

        public void Dispose() => _signal.Dispose();

        public async Task<CameraStatus> WaitForAsync(Func<CameraStatus, bool> match)
        {
            using var timeout = new CancellationTokenSource(Wait);
            while (true)
            {
                lock (_received)
                {
                    var found = _received.FirstOrDefault(match);
                    if (found is not null)
                    {
                        _received.Remove(found);
                        return found;
                    }
                }

                await _signal.WaitAsync(timeout.Token);
            }
        }
    }
}
