namespace Recam.Web.Recordings;

/// <summary>
/// The stretch of one day the timeline bar shows (1 minute to 3 hours), and the conversions
/// between a place on the bar and a time. Pure: the page keeps one and replaces it as the person
/// zooms and drags. Times are wall-clock times, like <see cref="TimelineSegment"/>.
/// </summary>
public sealed record TimelineWindow
{
    private TimelineWindow(DateTime day, TimelineZoom zoom, DateTime start)
    {
        Day = day;
        Zoom = zoom;
        Start = start;
    }

    /// <summary>Midnight of the day shown.</summary>
    public DateTime Day { get; }

    public TimelineZoom Zoom { get; }

    public DateTime Start { get; }

    public DateTime End => Start + Zoom.Span;

    public DateTime Center => Start + Zoom.Span / 2;

    public bool AtDayStart => Start <= Day;

    public bool AtDayEnd => End >= Day.AddDays(1);

    /// <summary>The ticks inside the window, on round multiples of the zoom's tick.</summary>
    public IReadOnlyList<DateTime> Ticks
    {
        get
        {
            var tick = Zoom.Tick.Ticks;
            var next = (long)Math.Ceiling((Start - Day).Ticks / (double)tick) * tick;
            var ticks = new List<DateTime>();
            for (; next <= (End - Day).Ticks; next += tick)
            {
                ticks.Add(Day.AddTicks(next));
            }

            return ticks;
        }
    }

    /// <summary>The window of <paramref name="zoom"/> centered on <paramref name="center"/>, kept inside the day.</summary>
    public static TimelineWindow Around(DateTime day, TimelineZoom zoom, DateTime center) =>
        new(day, zoom, Clamp(day, zoom, center - zoom.Span / 2));

    /// <summary>Where <paramref name="time"/> falls, as a fraction of the bar; outside the window it is off the bar.</summary>
    public double FractionOf(DateTime time) => (time - Start) / Zoom.Span;

    /// <summary>The time at <paramref name="fraction"/> of the bar, within the window.</summary>
    public DateTime TimeAt(double fraction) => Start + Zoom.Span * Math.Clamp(fraction, 0, 1);

    /// <summary>The window after dragging <paramref name="fraction"/> of the bar: dragging right shows earlier times.</summary>
    public TimelineWindow PanBy(double fraction) => new(Day, Zoom, Clamp(Day, Zoom, Start - Zoom.Span * fraction));

    /// <summary>One whole window earlier (negative) or later (positive).</summary>
    public TimelineWindow Step(int windows) => new(Day, Zoom, Clamp(Day, Zoom, Start + Zoom.Span * windows));

    /// <summary>The same center at another zoom.</summary>
    public TimelineWindow ZoomTo(TimelineZoom zoom) => Around(Day, zoom, Center);

    private static DateTime Clamp(DateTime day, TimelineZoom zoom, DateTime start)
    {
        var latest = day.AddDays(1) - zoom.Span;
        return start < day ? day : start > latest ? latest : start;
    }
}
