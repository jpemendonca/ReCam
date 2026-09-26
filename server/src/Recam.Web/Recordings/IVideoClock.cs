using Microsoft.AspNetCore.Components;

namespace Recam.Web.Recordings;

/// <summary>
/// Follows a recording as it plays, kept by the browser: the clock over the video and the line
/// on the timeline bar. Faked in tests.
/// </summary>
public interface IVideoClock
{
    /// <summary>
    /// Called when the video runs past the edge of the bar's window, with the time it reached, so
    /// the window can go along.
    /// </summary>
    event Action<DateTime>? HeadLeftWindow;

    /// <summary>A new file plays: the clock and the line count from <paramref name="fileStart"/>.</summary>
    Task FollowAsync(ElementReference video, ElementReference clock, ElementReference line, DateTime fileStart);

    /// <summary>Nothing plays any more: the line goes away.</summary>
    Task StopAsync();

    /// <summary>The bar shows <paramref name="window"/>, <paramref name="width"/> units wide.</summary>
    Task ShowWindowAsync(TimelineWindow window, double width);
}
