using Recam.Server.Domain;

namespace Recam.Server.Features.Recordings;

/// <summary>A camera's motion events of one UTC day, with the sensitivity that found them.</summary>
public sealed record MotionResponse(MotionSensitivity Sensitivity, IReadOnlyList<MotionEventResponse> Events);

/// <param name="Person">True or false once the detect service looked at the event; null when it did not
/// (not installed, not there yet, or too late).</param>
public sealed record MotionEventResponse(DateTimeOffset Start, DateTimeOffset End, double Peak, bool? Person);

public sealed record MotionSensitivityRequest(MotionSensitivity? Sensitivity);
