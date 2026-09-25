using System.Diagnostics.Metrics;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Recam.Server.Domain;
using Recam.Server.Features.Realtime;
using Recam.Server.Infrastructure.Observability;
using Recam.Server.Infrastructure.Realtime;
using Recam.Server.Tests.Support;

namespace Recam.Server.Tests.Infrastructure.Observability;

public sealed class RecamMetricsTests
{
    [Fact(DisplayName = "The gauges report online cameras, publishing cameras and active views")]
    public async Task Gauges_CameraPublishingAndWatched_ReportOneOfEach()
    {
        // arrange
        using var factory = new RecamApiFactory();
        var owner = await factory.PairDeviceAsync(DeviceRole.Owner);
        var camera = await factory.PairDeviceAsync(DeviceRole.Camera);
        await factory.PairDeviceAsync(DeviceRole.Camera, "Offline camera");
        await using var viewerConnection = await factory.ConnectAsync(owner.Credential);
        await using var cameraConnection = await factory.ConnectAsync(camera.Credential);
        await cameraConnection.InvokeAsync<HubResult>("ReportTelemetry", new TelemetryReport(50, true, null), TestContext.Current.CancellationToken);
        await cameraConnection.InvokeAsync<HubResult>("ReportPublishing", true, TestContext.Current.CancellationToken);
        await viewerConnection.InvokeAsync<HubResult>("WatchCamera", camera.DeviceId, TestContext.Current.CancellationToken);

        // act
        var values = Collect(factory);

        // assert
        Assert.Equal(1, values[RecamMetrics.CamerasOnline]);
        Assert.Equal(1, values[RecamMetrics.CamerasPublishing]);
        Assert.Equal(1, values[RecamMetrics.ViewsActive]);
    }

    [Fact(DisplayName = "The gauges drop to zero when the camera disconnects")]
    public async Task Gauges_CameraDisconnects_ReportZero()
    {
        // arrange
        using var factory = new RecamApiFactory();
        var owner = await factory.PairDeviceAsync(DeviceRole.Owner);
        var camera = await factory.PairDeviceAsync(DeviceRole.Camera);
        await using var viewerConnection = await factory.ConnectAsync(owner.Credential);
        var wentOffline = new TaskCompletionSource();
        viewerConnection.On<CameraStatus>("CameraStatusChanged", status =>
        {
            if (!status.Online)
            {
                wentOffline.TrySetResult();
            }
        });
        var cameraConnection = await factory.ConnectAsync(camera.Credential);
        await cameraConnection.InvokeAsync<HubResult>("ReportPublishing", true, TestContext.Current.CancellationToken);
        var whileConnected = Collect(factory)[RecamMetrics.CamerasOnline];

        // act
        await cameraConnection.DisposeAsync();
        await wentOffline.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        var values = Collect(factory);

        // assert
        Assert.Equal(1, whileConnected);
        Assert.Equal(0, values[RecamMetrics.CamerasOnline]);
        Assert.Equal(0, values[RecamMetrics.CamerasPublishing]);
    }

    // Reads the gauges of this server only: other tests run their own servers in parallel.
    private static Dictionary<string, int> Collect(RecamApiFactory factory)
    {
        var meterFactory = factory.Services.GetRequiredService<IMeterFactory>();
        var values = new Dictionary<string, int>();
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (instrument.Meter.Name == RecamMetrics.MeterName && instrument.Meter.Scope == meterFactory)
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<int>((instrument, value, _, _) => values[instrument.Name] = value);
        listener.Start();
        listener.RecordObservableInstruments();
        return values;
    }
}
