using Microsoft.AspNetCore.Components;

namespace Recam.Web.Recordings;

/// <summary>
/// Follows a recording as it plays, kept by the browser: the clock over the video, and the moment
/// it reached, so the timeline bar keeps it under its middle line (bullet 13.4). Faked in tests.
/// </summary>
public interface IVideoClock
{
    /// <summary>The video reached another second, as a wall-clock time.</summary>
    event Action<DateTime>? HeadMoved;

    /// <summary>A new file plays: the clock counts from <paramref name="fileStart"/>.</summary>
    Task FollowAsync(ElementReference video, ElementReference clock, DateTime fileStart);

    /// <summary>Nothing plays any more.</summary>
    Task StopAsync();
}
