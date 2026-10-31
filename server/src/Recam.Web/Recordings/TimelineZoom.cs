namespace Recam.Web.Recordings;

/// <summary>How much time the timeline bar shows at once, and how far apart its ticks are.</summary>
public sealed record TimelineZoom(string Name, TimeSpan Span, TimeSpan Tick)
{
    public static readonly TimelineZoom Minute = new("minute", TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(10));

    public static readonly TimelineZoom FiveMinutes = new("five-minutes", TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(1));

    public static readonly TimelineZoom HalfHour = new("half-hour", TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(5));

    public static readonly TimelineZoom Hour = new("hour", TimeSpan.FromHours(1), TimeSpan.FromMinutes(10));

    public static readonly TimelineZoom Day = new("day", TimeSpan.FromDays(1), TimeSpan.FromHours(3));

    public static IReadOnlyList<TimelineZoom> All { get; } = [Minute, FiveMinutes, HalfHour, Hour, Day];
}
