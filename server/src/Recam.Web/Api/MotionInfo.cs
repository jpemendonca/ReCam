namespace Recam.Web.Api;

/// <summary>A camera's motion in one UTC day, and the sensitivity that found it ("low", "medium", "high").</summary>
public sealed record MotionInfo(string Sensitivity, IReadOnlyList<MotionEventInfo> Events);

public sealed record MotionEventInfo(DateTimeOffset Start, DateTimeOffset End, double Peak);
