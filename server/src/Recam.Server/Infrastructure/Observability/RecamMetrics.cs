using System.Diagnostics.Metrics;
using Recam.Server.Infrastructure.Presence;

namespace Recam.Server.Infrastructure.Observability;

/// <summary>
/// The server's own gauges, read from <see cref="DevicePresence"/> each time a listener
/// collects. Nothing is counted twice: presence is already the source of truth.
/// </summary>
public sealed class RecamMetrics
{
    public const string MeterName = "Recam.Server";
    public const string CamerasOnline = "recam.cameras.online";
    public const string CamerasPublishing = "recam.cameras.publishing";
    public const string ViewsActive = "recam.views.active";

    public RecamMetrics(IMeterFactory meterFactory, DevicePresence presence)
    {
        var meter = meterFactory.Create(MeterName);
        meter.CreateObservableGauge(
            CamerasOnline, () => presence.OnlineCameras, "{camera}", "Cameras connected to the hub.");
        meter.CreateObservableGauge(
            CamerasPublishing, () => presence.PublishingCameras, "{camera}", "Cameras sending video.");
        meter.CreateObservableGauge(
            ViewsActive, () => presence.ActiveViews, "{view}", "Live views open across all cameras.");
    }
}
