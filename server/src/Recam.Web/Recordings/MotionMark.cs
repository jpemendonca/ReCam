namespace Recam.Web.Recordings;

/// <summary>
/// A motion event placed on this computer's clock, like <see cref="TimelineSegment"/>. <see cref="Person"/>
/// is null when the optional detect service did not look at it.
/// </summary>
public sealed record MotionMark(DateTime Start, DateTime End, bool? Person = null);
