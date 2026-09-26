namespace Recam.Web.Recordings;

/// <summary>A motion event placed on this computer's clock, like <see cref="TimelineSegment"/>.</summary>
public sealed record MotionMark(DateTime Start, DateTime End);
