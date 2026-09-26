using Microsoft.AspNetCore.Components;

namespace Recam.Web.Recordings;

/// <summary>The clock over a recording, kept by the browser as the video plays. Faked in tests.</summary>
public interface IVideoClock
{
    /// <summary>Shows <paramref name="fileStart"/> plus the video's position in <paramref name="label"/>.</summary>
    Task FollowAsync(ElementReference video, ElementReference label, DateTime fileStart);
}
