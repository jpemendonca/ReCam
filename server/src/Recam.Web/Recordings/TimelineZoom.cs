namespace Recam.Web.Recordings;

/// <summary>How much of the day the timeline bar shows at once, and how far apart its ticks are.</summary>
public sealed record TimelineZoom(string Name, TimeSpan Span, TimeSpan Tick)
{
    public static readonly TimelineZoom Minute = new("minute", TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(10));

    public static readonly TimelineZoom QuarterHour = new("quarter-hour", TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(3));

    public static readonly TimelineZoom Hour = new("hour", TimeSpan.FromHours(1), TimeSpan.FromMinutes(10));

    public static readonly TimelineZoom ThreeHours = new("three-hours", TimeSpan.FromHours(3), TimeSpan.FromMinutes(30));

    public static IReadOnlyList<TimelineZoom> All { get; } = [Minute, QuarterHour, Hour, ThreeHours];
}
