using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;
using Recam.Server.Domain;
using Recam.Server.Features.Realtime;
using Recam.Server.Infrastructure.Realtime;
using Recam.Server.Tests.Support;

namespace Recam.Server.Tests.Features.Realtime;

public sealed class WatchLeasesTests
{
    private static async Task<(RecamApiFactory Factory, HubConnection Viewer, HubConnection Camera, Guid CameraId)> ConnectBothAsync()
    {
        var factory = new RecamApiFactory();
        var owner = await factory.PairDeviceAsync(DeviceRole.Owner);
        var camera = await factory.PairDeviceAsync(DeviceRole.Camera);
        var viewerConnection = await factory.ConnectAsync(owner.Credential);
        var cameraConnection = await factory.ConnectAsync(camera.Credential);

        // The server runs OnConnectedAsync before any invocation, so this round trip means the
        // camera is fully registered and will not also get a StartPublishing from connecting.
        await cameraConnection.InvokeAsync<HubResult>("ReportTelemetry", 50, true, TestContext.Current.CancellationToken);
        return (factory, viewerConnection, cameraConnection, camera.DeviceId);
    }

    [Fact(DisplayName = "The first viewer makes the camera start publishing")]
    public async Task WatchCamera_FirstLease_SendsStartPublishing()
    {
        // arrange
        var (factory, viewer, camera, cameraId) = await ConnectBothAsync();
        using var _ = factory;
        await using var viewerConnection = viewer;
        await using var cameraConnection = camera;
        using var starts = new HubCallCounter(cameraConnection, "StartPublishing");

        // act
        var result = await viewerConnection.InvokeAsync<HubResult>("WatchCamera", cameraId, TestContext.Current.CancellationToken);

        // assert
        Assert.True(result.Ok);
        await starts.WaitForCallAsync();
        Assert.Equal(1, starts.Count);
    }

    [Fact(DisplayName = "When the last viewer leaves, the camera stops only after the grace period")]
    public async Task UnwatchCamera_LastLease_SendsStopAfterGrace()
    {
        // arrange
        var (factory, viewer, camera, cameraId) = await ConnectBothAsync();
        using var _ = factory;
        await using var viewerConnection = viewer;
        await using var cameraConnection = camera;
        using var stops = new HubCallCounter(cameraConnection, "StopPublishing");
        await viewerConnection.InvokeAsync<HubResult>("WatchCamera", cameraId, TestContext.Current.CancellationToken);

        // act
        await viewerConnection.InvokeAsync<HubResult>("UnwatchCamera", cameraId, TestContext.Current.CancellationToken);
        var silentDuringGrace = await stops.StaysSilentAsync();
        factory.Time.Advance(WatchLeases.StopGrace);

        // assert
        Assert.True(silentDuringGrace);
        await stops.WaitForCallAsync();
    }

    [Fact(DisplayName = "Watching again during the grace period keeps the stream running")]
    public async Task WatchCamera_DuringGrace_CancelsStop()
    {
        // arrange
        var (factory, viewer, camera, cameraId) = await ConnectBothAsync();
        using var _ = factory;
        await using var viewerConnection = viewer;
        await using var cameraConnection = camera;
        using var starts = new HubCallCounter(cameraConnection, "StartPublishing");
        using var stops = new HubCallCounter(cameraConnection, "StopPublishing");
        await viewerConnection.InvokeAsync<HubResult>("WatchCamera", cameraId, TestContext.Current.CancellationToken);
        await starts.WaitForCallAsync();
        await viewerConnection.InvokeAsync<HubResult>("UnwatchCamera", cameraId, TestContext.Current.CancellationToken);
        factory.Time.Advance(TimeSpan.FromSeconds(10));

        // act
        await viewerConnection.InvokeAsync<HubResult>("WatchCamera", cameraId, TestContext.Current.CancellationToken);
        factory.Time.Advance(WatchLeases.StopGrace);

        // assert
        Assert.True(await stops.StaysSilentAsync());
        Assert.Equal(1, starts.Count);
    }

    [Fact(DisplayName = "A camera that reconnects while watched starts publishing again")]
    public async Task CameraReconnect_WithOpenLease_SendsStartPublishing()
    {
        // arrange
        var factory = new RecamApiFactory();
        using var _ = factory;
        var owner = await factory.PairDeviceAsync(DeviceRole.Owner);
        var camera = await factory.PairDeviceAsync(DeviceRole.Camera);
        await using var viewerConnection = await factory.ConnectAsync(owner.Credential);
        await viewerConnection.InvokeAsync<HubResult>("WatchCamera", camera.DeviceId, TestContext.Current.CancellationToken);

        // act
        var reconnect = new HubConnectionBuilderProbe(factory, camera.Credential);
        await using var cameraConnection = await reconnect.ConnectListeningForStartAsync();

        // assert
        await reconnect.Starts.WaitForCallAsync();
        reconnect.Starts.Dispose();
    }

    [Fact(DisplayName = "Watching something that is not a camera is refused as data")]
    public async Task WatchCamera_UnknownCamera_ReturnsNotFound()
    {
        // arrange
        using var factory = new RecamApiFactory();
        var owner = await factory.PairDeviceAsync(DeviceRole.Owner);
        await using var viewerConnection = await factory.ConnectAsync(owner.Credential);

        // act
        var result = await viewerConnection.InvokeAsync<HubResult>("WatchCamera", Guid.NewGuid(), TestContext.Current.CancellationToken);

        // assert
        Assert.False(result.Ok);
        Assert.Equal(MediaErrors.CameraNotFound.Code, result.Code);
    }

    [Fact(DisplayName = "A camera's publishing report shows in the camera list")]
    public async Task ReportPublishing_True_ShowsPublishingInList()
    {
        // arrange
        using var factory = new RecamApiFactory();
        var owner = await factory.PairDeviceAsync(DeviceRole.Owner);
        var camera = await factory.PairDeviceAsync(DeviceRole.Camera);
        await using var cameraConnection = await factory.ConnectAsync(camera.Credential);

        // act
        await cameraConnection.InvokeAsync<HubResult>("ReportPublishing", true, TestContext.Current.CancellationToken);

        // assert
        using var client = factory.CreateDeviceClient(owner.Credential);
        var cameras = await client.GetFromJsonAsync<List<CameraStatus>>(
            new Uri("/api/cameras", UriKind.Relative), ApiJson.Options, TestContext.Current.CancellationToken);
        Assert.True(Assert.Single(cameras!).Publishing);
    }

    /// <summary>Connects a camera with the StartPublishing handler registered before the connection opens.</summary>
    private sealed class HubConnectionBuilderProbe(RecamApiFactory factory, string credential)
    {
        public HubCallCounter Starts { get; private set; } = null!;

        public async Task<HubConnection> ConnectListeningForStartAsync()
        {
            var connection = factory.BuildConnection(credential);
            Starts = new HubCallCounter(connection, "StartPublishing");
            await connection.StartAsync(TestContext.Current.CancellationToken);
            return connection;
        }
    }

    [Fact(DisplayName = "The camera hears how many viewers are watching it")]
    public async Task WatchCamera_TwoViewers_SendsWatcherCountToCamera()
    {
        // arrange
        var (factory, viewer, camera, cameraId) = await ConnectBothAsync();
        using var _ = factory;
        await using var viewerConnection = viewer;
        await using var cameraConnection = camera;
        var viewerDevice = await factory.PairDeviceAsync(DeviceRole.Viewer, "Second viewer");
        await using var secondViewer = await factory.ConnectAsync(viewerDevice.Credential);
        using var counts = new CountInbox(cameraConnection);

        // act
        await viewerConnection.InvokeAsync<HubResult>("WatchCamera", cameraId, TestContext.Current.CancellationToken);
        await secondViewer.InvokeAsync<HubResult>("WatchCamera", cameraId, TestContext.Current.CancellationToken);
        await secondViewer.InvokeAsync<HubResult>("UnwatchCamera", cameraId, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal([1, 2, 1], await counts.WaitForAsync(3));
    }

    [Fact(DisplayName = "A viewer that disconnects no longer counts as watching")]
    public async Task ViewerDisconnect_WithOpenLease_SendsLowerCount()
    {
        // arrange
        var (factory, viewer, camera, cameraId) = await ConnectBothAsync();
        using var _ = factory;
        await using var cameraConnection = camera;
        using var counts = new CountInbox(cameraConnection);
        await viewer.InvokeAsync<HubResult>("WatchCamera", cameraId, TestContext.Current.CancellationToken);

        // act
        await viewer.DisposeAsync();

        // assert
        Assert.Equal([1, 0], await counts.WaitForAsync(2));
    }

    [Fact(DisplayName = "A camera learns the current count as soon as it connects")]
    public async Task CameraConnect_SendsCurrentWatcherCount()
    {
        // arrange
        using var factory = new RecamApiFactory();
        var camera = await factory.PairDeviceAsync(DeviceRole.Camera);
        var cameraConnection = factory.BuildConnection(camera.Credential);
        await using var __ = cameraConnection;
        using var counts = new CountInbox(cameraConnection);

        // act
        await cameraConnection.StartAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.Equal([0], await counts.WaitForAsync(1));
    }

    /// <summary>Collects the counts a camera receives in WatchersChanged.</summary>
    private sealed class CountInbox : IDisposable
    {
        private static readonly TimeSpan Wait = TimeSpan.FromSeconds(10);

        private readonly List<int> _received = [];
        private readonly SemaphoreSlim _signal = new(0);

        public CountInbox(HubConnection connection) =>
            connection.On<int>("WatchersChanged", count =>
            {
                lock (_received)
                {
                    _received.Add(count);
                }

                _signal.Release();
            });

        public void Dispose() => _signal.Dispose();

        public async Task<List<int>> WaitForAsync(int messages)
        {
            using var timeout = new CancellationTokenSource(Wait);
            for (var i = 0; i < messages; i++)
            {
                await _signal.WaitAsync(timeout.Token);
            }

            lock (_received)
            {
                return [.. _received];
            }
        }
    }
}
