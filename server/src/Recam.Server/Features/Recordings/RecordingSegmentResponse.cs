namespace Recam.Server.Features.Recordings;

/// <summary>One playable file; <see cref="Url"/> is relative to the server.</summary>
public sealed record RecordingSegmentResponse(DateTimeOffset Start, DateTimeOffset End, string Url);
