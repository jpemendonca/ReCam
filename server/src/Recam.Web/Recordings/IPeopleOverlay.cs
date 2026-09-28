using Microsoft.AspNetCore.Components;

namespace Recam.Web.Recordings;

/// <summary>
/// Draws the boxes of <see cref="PersonTrack"/> over a playing &lt;video&gt;, in the browser, as
/// the video plays. Faked in tests.
/// </summary>
public interface IPeopleOverlay
{
    /// <summary>Draws <paramref name="track"/>'s boxes over <paramref name="video"/>, in <paramref name="boxes"/>.</summary>
    Task FollowAsync(ElementReference video, ElementReference boxes, PersonTrack track);

    /// <summary>No boxes any more: the file has none, or the person turned them off.</summary>
    Task StopAsync();

    /// <summary>Shows the player, video and boxes, on the whole screen.</summary>
    Task FullScreenAsync(ElementReference player);
}
