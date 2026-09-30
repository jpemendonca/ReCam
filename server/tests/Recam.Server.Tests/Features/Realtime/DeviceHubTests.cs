using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Recam.Server.Domain;
using Recam.Server.Features.Realtime;
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
        using var updates = await StatusInbox.OpenAsync(viewerConnection);
        await using var cameraConnection = await factory.ConnectAsync(camera.Credential);
        await updates.WaitForAsync(status => status.Online);

        // act
        var result = await cameraConnection.InvokeAsync<HubResult>(
            "ReportTelemetry", new TelemetryReport(42, true, null), TestContext.Current.CancellationToken);

        // assert
        Assert.True(result.Ok);
        var status = await updates.WaitForAsync(status => status.BatteryLevel == 42);
        Assert.Equal(camera.DeviceId, status.Id);
        Assert.True(status.IsCharging);
        using var client = factory.CreateDeviceClient(owner.Credential);
        var cameras = await client.GetFromJsonAsync<List<CameraStatus>>(CamerasUri, ApiJson.Options, TestContext.Current.CancellationToken);
        Assert.Equal(42, Assert.Single(cameras!).BatteryLevel);
    }

    [Fact(DisplayName = "The camera's temperature reaches viewers and the camera list")]
    public async Task ReportTelemetry_WithTemperature_ReachesViewersAndList()
    {
        // arrange
        using var factory = new RecamApiFactory();
        var owner = await factory.PairDeviceAsync(DeviceRole.Owner);
        var camera = await factory.PairDeviceAsync(DeviceRole.Camera, "Porch");
        await using var viewerConnection = await factory.ConnectAsync(owner.Credential);
        using var updates = await StatusInbox.OpenAsync(viewerConnection);
        await using var cameraConnection = await factory.ConnectAsync(camera.Credential);

        // act
        var result = await cameraConnection.InvokeAsync<HubResult>(
            "ReportTelemetry", new TelemetryReport(70, false, 38.4), TestContext.Current.CancellationToken);

        // assert
        Assert.True(result.Ok);
        var status = await updates.WaitForAsync(status => status.TemperatureC is not null);
        Assert.Equal(38.4, status.TemperatureC);
        using var client = factory.CreateDeviceClient(owner.Credential);
        var cameras = await client.GetFromJsonAsync<List<CameraStatus>>(CamerasUri, ApiJson.Options, TestContext.Current.CancellationToken);
        Assert.Equal(38.4, Assert.Single(cameras!).TemperatureC);
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
            "ReportTelemetry", new TelemetryReport(101, false, null), TestContext.Current.CancellationToken);

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
            "ReportTelemetry", new TelemetryReport(50, false, null), TestContext.Current.CancellationToken));

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
        using var updates = await StatusInbox.OpenAsync(viewerConnection);
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

    [Fact(DisplayName = "A camera that drops right while it connects still ends up offline")]
    public async Task Cameras_DropDuringConnect_EndsOffline()
    {
        // arrange
        using var factory = new RecamApiFactory();
        var owner = await factory.PairDeviceAsync(DeviceRole.Owner);
        var camera = await factory.PairDeviceAsync(DeviceRole.Camera);
        using var client = factory.CreateDeviceClient(owner.Credential);

        // act
        // No wait between the two: the drop lands while the server still registers the camera.
        var cameraConnection = await factory.ConnectAsync(camera.Credential);
        await cameraConnection.DisposeAsync();

        // assert
        using var timeout = new CancellationTokenSource(Wait);
        while (true)
        {
            var cameras = await client.GetFromJsonAsync<List<CameraStatus>>(CamerasUri, ApiJson.Options, timeout.Token);
            if (!Assert.Single(cameras!).Online)
            {
                break;
            }

            await Task.Delay(50, timeout.Token);
        }
    }

    [Fact(DisplayName = "Monitors hear the device list changed when a device pairs, connects, drops and is removed")]
    public async Task DevicesChanged_OnPairConnectDropRemove_ReachesMonitors()
    {
        // arrange
        using var factory = new RecamApiFactory();
        var owner = await factory.PairDeviceAsync(DeviceRole.Owner);
        await using var viewerConnection = await factory.ConnectAsync(owner.Credential);
        var changes = new SemaphoreSlim(0);
        viewerConnection.On("DevicesChanged", () => changes.Release());
        async Task HeardAsync(string when) =>
            Assert.True(await changes.WaitAsync(Wait, TestContext.Current.CancellationToken), when);

        // act
        var monitor = await factory.PairDeviceAsync(DeviceRole.Viewer);
        await HeardAsync("after pairing");
        var monitorConnection = await factory.ConnectAsync(monitor.Credential);
        await HeardAsync("after connecting");
        await monitorConnection.DisposeAsync();
        await HeardAsync("after dropping");
        using var client = factory.CreateDeviceClient(owner.Credential);
        using var removed = await client.DeleteAsync(new Uri($"/api/devices/{monitor.DeviceId}", UriKind.Relative), TestContext.Current.CancellationToken);

        // assert
        await HeardAsync("after removing");
        Assert.Equal(HttpStatusCode.NoContent, removed.StatusCode);
    }

    [Fact(DisplayName = "The torch cannot be switched while the camera is not sending video")]
    public async Task SetTorch_WhenCameraNotPublishing_ReturnsError()
    {
        // arrange
        using var factory = new RecamApiFactory();
        var owner = await factory.PairDeviceAsync(DeviceRole.Owner);
        var camera = await factory.PairDeviceAsync(DeviceRole.Camera);
        await using var cameraConnection = await factory.ConnectAsync(camera.Credential);
        using var torch = new TorchInbox(cameraConnection, "SetTorch");
        await using var viewerConnection = await factory.ConnectAsync(owner.Credential);

        // act
        var result = await viewerConnection.InvokeAsync<HubResult>(
            "SetTorch", camera.DeviceId, true, TestContext.Current.CancellationToken);

        // assert
        Assert.False(result.Ok);
        Assert.Equal(MediaErrors.CameraNotPublishing.Code, result.Code);
        Assert.True(await torch.StaysSilentAsync());
    }

    [Fact(DisplayName = "A viewer's torch command reaches the publishing camera")]
    public async Task SetTorch_WhenPublishing_ForwardsToCamera()
    {
        // arrange
        using var factory = new RecamApiFactory();
        var owner = await factory.PairDeviceAsync(DeviceRole.Owner);
        var camera = await factory.PairDeviceAsync(DeviceRole.Camera);
        await using var cameraConnection = await factory.ConnectAsync(camera.Credential);
        using var torch = new TorchInbox(cameraConnection, "SetTorch");
        await cameraConnection.InvokeAsync<HubResult>("ReportPublishing", true, TestContext.Current.CancellationToken);
        await using var viewerConnection = await factory.ConnectAsync(owner.Credential);

        // act
        var result = await viewerConnection.InvokeAsync<HubResult>(
            "SetTorch", camera.DeviceId, true, TestContext.Current.CancellationToken);

        // assert
        Assert.True(result.Ok);
        Assert.True(await torch.WaitForAsync());
    }

    [Fact(DisplayName = "A torch command for an unknown camera is refused")]
    public async Task SetTorch_ForUnknownCamera_ReturnsNotFound()
    {
        // arrange
        using var factory = new RecamApiFactory();
        var owner = await factory.PairDeviceAsync(DeviceRole.Owner);
        await using var viewerConnection = await factory.ConnectAsync(owner.Credential);

        // act
        var result = await viewerConnection.InvokeAsync<HubResult>(
            "SetTorch", Guid.NewGuid(), true, TestContext.Current.CancellationToken);

        // assert
        Assert.False(result.Ok);
        Assert.Equal(MediaErrors.CameraNotFound.Code, result.Code);
    }

    [Fact(DisplayName = "Only viewers switch the torch")]
    public async Task SetTorch_FromCamera_IsRejected()
    {
        // arrange
        using var factory = new RecamApiFactory();
        var camera = await factory.PairDeviceAsync(DeviceRole.Camera);
        await using var connection = await factory.ConnectAsync(camera.Credential);

        // act
        var exception = await Record.ExceptionAsync(() => connection.InvokeAsync<HubResult>(
            "SetTorch", camera.DeviceId, true, TestContext.Current.CancellationToken));

        // assert
        Assert.IsType<HubException>(exception);
    }

    [Fact(DisplayName = "What the camera reports about its torch reaches the viewers")]
    public async Task ReportTorch_FromCamera_NotifiesViewers()
    {
        // arrange
        using var factory = new RecamApiFactory();
        var owner = await factory.PairDeviceAsync(DeviceRole.Owner);
        var camera = await factory.PairDeviceAsync(DeviceRole.Camera);
        await using var viewerConnection = await factory.ConnectAsync(owner.Credential);
        var changes = new List<(Guid CameraId, bool On)>();
        using var received = new SemaphoreSlim(0);
        viewerConnection.On<Guid, bool>("TorchChanged", (cameraId, on) =>
        {
            changes.Add((cameraId, on));
            received.Release();
        });
        await using var cameraConnection = await factory.ConnectAsync(camera.Credential);

        // act
        var result = await cameraConnection.InvokeAsync<HubResult>("ReportTorch", true, TestContext.Current.CancellationToken);

        // assert
        Assert.True(result.Ok);
        Assert.True(await received.WaitAsync(Wait, TestContext.Current.CancellationToken));
        Assert.Equal((camera.DeviceId, true), Assert.Single(changes));
    }

    [Fact(DisplayName = "The server remembers the torch the camera reported, and forgets it when the camera stops sending video")]
    public async Task ReportTorch_ThenStopPublishing_ListShowsTorchUntilThen()
    {
        // arrange
        using var factory = new RecamApiFactory();
        var owner = await factory.PairDeviceAsync(DeviceRole.Owner);
        var camera = await factory.PairDeviceAsync(DeviceRole.Camera);
        await using var cameraConnection = await factory.ConnectAsync(camera.Credential);
        await cameraConnection.InvokeAsync<HubResult>("ReportPublishing", true, TestContext.Current.CancellationToken);
        using var client = factory.CreateDeviceClient(owner.Credential);

        // act
        await cameraConnection.InvokeAsync<HubResult>("ReportTorch", true, TestContext.Current.CancellationToken);
        var lit = await client.GetFromJsonAsync<List<CameraStatus>>(CamerasUri, ApiJson.Options, TestContext.Current.CancellationToken);
        await cameraConnection.InvokeAsync<HubResult>("ReportPublishing", false, TestContext.Current.CancellationToken);
        var stopped = await client.GetFromJsonAsync<List<CameraStatus>>(CamerasUri, ApiJson.Options, TestContext.Current.CancellationToken);

        // assert
        Assert.True(Assert.Single(lit!).TorchOn);
        Assert.False(Assert.Single(stopped!).TorchOn);
    }

    [Fact(DisplayName = "A camera that goes offline has its torch off")]
    public async Task ReportTorch_ThenDisconnect_TorchOff()
    {
        // arrange
        using var factory = new RecamApiFactory();
        var owner = await factory.PairDeviceAsync(DeviceRole.Owner);
        var camera = await factory.PairDeviceAsync(DeviceRole.Camera);
        var cameraConnection = await factory.ConnectAsync(camera.Credential);
        await cameraConnection.InvokeAsync<HubResult>("ReportPublishing", true, TestContext.Current.CancellationToken);
        await cameraConnection.InvokeAsync<HubResult>("ReportTorch", true, TestContext.Current.CancellationToken);
        await using var viewerConnection = await factory.ConnectAsync(owner.Credential);
        using var updates = await StatusInbox.OpenAsync(viewerConnection);

        // act
        await cameraConnection.DisposeAsync();

        // assert
        var status = await updates.WaitForAsync(status => !status.Online);
        Assert.False(status.TorchOn);
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

    /// <summary>Records the value of a one-argument bool message a client receives.</summary>
    [Fact(DisplayName = "When the first recording file arrives, viewers hear the camera is recording")]
    public async Task RecordingState_FileArrives_ViewersHearRecording()
    {
        // arrange
        using var factory = new RecamApiFactory();
        var owner = await factory.PairDeviceAsync(DeviceRole.Owner);
        var camera = await factory.PairDeviceAsync(DeviceRole.Camera);
        await using var viewerConnection = await factory.ConnectAsync(owner.Credential);
        using var updates = await StatusInbox.OpenAsync(viewerConnection);
        await using var cameraConnection = await factory.ConnectAsync(camera.Credential);
        var worker = factory.Services.GetRequiredService<RecordingStateWorker>();
        await worker.CheckAsync(TestContext.Current.CancellationToken);
        var starting = await updates.WaitForAsync(status => status.RecordingState == RecordingState.Starting);

        // act
        var segment = RecordingFiles.Write(factory.RecordingsDirectory, camera.DeviceId, factory.Time.GetUtcNow(), 1000);
        File.SetLastWriteTimeUtc(segment, factory.Time.GetUtcNow().UtcDateTime);
        await worker.CheckAsync(TestContext.Current.CancellationToken);

        // assert
        var recording = await updates.WaitForAsync(status => status.RecordingState == RecordingState.Recording);
        Assert.True(starting.Recording);
        Assert.Equal(camera.DeviceId, recording.Id);
    }

    private sealed class TorchInbox : IDisposable
    {
        private readonly SemaphoreSlim _signal = new(0);
        private bool _last;

        public TorchInbox(HubConnection connection, string method) =>
            connection.On<bool>(method, on =>
            {
                Volatile.Write(ref _last, on);
                _signal.Release();
            });

        public void Dispose() => _signal.Dispose();

        /// <summary>Waits for the next message and returns its value.</summary>
        public async Task<bool> WaitForAsync()
        {
            using var timeout = new CancellationTokenSource(Wait);
            await _signal.WaitAsync(timeout.Token);
            return Volatile.Read(ref _last);
        }

        public async Task<bool> StaysSilentAsync() => !await _signal.WaitAsync(TimeSpan.FromMilliseconds(500));
    }

    /// <summary>Collects CameraStatusChanged messages and waits for one that matches.</summary>
    private sealed class StatusInbox : IDisposable
    {
        private readonly List<CameraStatus> _received = [];
        private readonly SemaphoreSlim _signal = new(0);

        private StatusInbox(HubConnection connection) =>
            connection.On<CameraStatus>("CameraStatusChanged", status =>
            {
                lock (_received)
                {
                    _received.Add(status);
                }

                _signal.Release();
            });

        /// <summary>
        /// StartAsync returns before the server runs OnConnectedAsync, which adds the viewer to the
        /// group that gets status changes. The hub runs invocations only after OnConnectedAsync, so
        /// one round trip means the viewer is in the group and no change is missed.
        /// </summary>
        public static async Task<StatusInbox> OpenAsync(HubConnection connection)
        {
            var inbox = new StatusInbox(connection);
            await connection.InvokeAsync<HubResult>("UnwatchCamera", Guid.Empty, TestContext.Current.CancellationToken);
            return inbox;
        }

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
