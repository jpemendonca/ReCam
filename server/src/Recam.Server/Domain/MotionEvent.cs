namespace Recam.Server.Domain;

/// <summary>A stretch of motion in the recordings; <see cref="Peak"/> is the most of the picture that changed.</summary>
public sealed record MotionEvent(DateTimeOffset Start, DateTimeOffset End, double Peak);
