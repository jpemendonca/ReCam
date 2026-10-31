namespace Recam.Web.Recordings;

/// <summary>
/// What the timeline bar shows: the zoom's span around a center, the moment under the fixed line
/// in the middle of the bar (bullet 13.4). The center stays within the day; the window may reach
/// past midnight, where nothing is drawn. Pure: the page keeps one and replaces it as the video
/// plays, the person drags and zooms. Times are wall-clock times, like <see cref="TimelineSegment"/>.
/// </summary>
public sealed record TimelineWindow
{
    private TimelineWindow(DateTime day, TimelineZoom zoom, DateTime center)
    {
        Day = day;
        Zoom = zoom;
        Center = center;
    }

    /// <summary>Midnight of the day shown.</summary>
    public DateTime Day { get; }

    public TimelineZoom Zoom { get; }

    /// <summary>The moment under the line in the middle of the bar.</summary>
    public DateTime Center { get; }

    public DateTime Start => Center - Zoom.Span / 2;

    public DateTime End => Center + Zoom.Span / 2;

    /// <summary>The ticks inside the window and the day, on round multiples of the zoom's tick.</summary>
    public IReadOnlyList<DateTime> Ticks
    {
        get
        {
            var tick = Zoom.Tick.Ticks;
            var from = Start < Day ? Day : Start;
            var to = End > Day.AddDays(1) ? Day.AddDays(1) : End;
            var next = (long)Math.Ceiling((from - Day).Ticks / (double)tick) * tick;
            var ticks = new List<DateTime>();
            for (; next <= (to - Day).Ticks; next += tick)
            {
                ticks.Add(Day.AddTicks(next));
            }

            return ticks;
        }
    }

    /// <summary>The window of <paramref name="zoom"/> centered on <paramref name="center"/>, kept within the day.</summary>
    public static TimelineWindow Around(DateTime day, TimelineZoom zoom, DateTime center) => new(day, zoom, Clamp(day, center));

    /// <summary>Where <paramref name="time"/> falls, as a fraction of the bar; outside the window it is off the bar.</summary>
    public double FractionOf(DateTime time) => (time - Start) / Zoom.Span;

    /// <summary>The time at <paramref name="fraction"/> of the bar.</summary>
    public DateTime TimeAt(double fraction) => Start + Zoom.Span * Math.Clamp(fraction, 0, 1);

    /// <summary>The window after dragging <paramref name="fraction"/> of the bar: dragging right shows earlier times.</summary>
    public TimelineWindow PanBy(double fraction) => Around(Day, Zoom, Center - Zoom.Span * fraction);

    /// <summary>The same center at another zoom.</summary>
    public TimelineWindow ZoomTo(TimelineZoom zoom) => Around(Day, zoom, Center);

    private static DateTime Clamp(DateTime day, DateTime center) =>
        center < day ? day : center > day.AddDays(1) ? day.AddDays(1) : center;
}
