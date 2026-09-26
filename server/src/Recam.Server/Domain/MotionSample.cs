namespace Recam.Server.Domain;

/// <summary>What the motion service measured at one instant: the fraction of the picture that changed.</summary>
public sealed record MotionSample(DateTimeOffset At, double Changed);
