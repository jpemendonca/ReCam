using Recam.Server.Domain;

namespace Recam.Server.Features.Recordings;

/// <summary>A camera's motion events of one UTC day, with the sensitivity that found them.</summary>
public sealed record MotionResponse(MotionSensitivity Sensitivity, IReadOnlyList<MotionEventResponse> Events);

public sealed record MotionEventResponse(DateTimeOffset Start, DateTimeOffset End, double Peak);

public sealed record MotionSensitivityRequest(MotionSensitivity? Sensitivity);
