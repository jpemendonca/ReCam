using Microsoft.AspNetCore.Components;

namespace Recam.Web.Recordings;

/// <summary>
/// Taps and drags on the timeline bar, followed by the browser (wwwroot/js/timeline.js) so a finger
/// or the mouse can leave the bar while dragging. Positions are fractions of the bar's width.
/// Faked in tests.
/// </summary>
public interface ITimelineGestures
{
    /// <summary>A tap, at this fraction of the bar from its left edge.</summary>
    event Action<double>? Tapped;

    /// <summary>A drag going on: how far it moved since it started, as a fraction of the bar; right is positive.</summary>
    event Action<double>? Dragging;

    /// <summary>The drag ended, this far from where it started.</summary>
    event Action<double>? Dropped;

    /// <summary>The mouse is over the bar, at this fraction from its left edge; null when it left.</summary>
    event Action<double?>? Hovering;

    /// <summary>Starts following the bar's gestures.</summary>
    Task AttachAsync(ElementReference bar);
}
