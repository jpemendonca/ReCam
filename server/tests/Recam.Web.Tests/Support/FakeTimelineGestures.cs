using Microsoft.AspNetCore.Components;
using Recam.Web.Recordings;

namespace Recam.Web.Tests.Support;

/// <summary>Plays the person's taps and drags on the timeline bar, as timeline.js reports them.</summary>
public sealed class FakeTimelineGestures : ITimelineGestures
{
    public event Action<double>? Tapped;

    public event Action<double>? Dragging;

    public event Action<double>? Dropped;

    public int Attached { get; private set; }

    public Task AttachAsync(ElementReference bar)
    {
        Attached++;
        return Task.CompletedTask;
    }

    public void Tap(double fraction) => Tapped?.Invoke(fraction);

    public void Drag(double fraction) => Dragging?.Invoke(fraction);

    public void Drop(double fraction) => Dropped?.Invoke(fraction);
}
