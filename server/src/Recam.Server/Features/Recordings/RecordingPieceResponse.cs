namespace Recam.Server.Features.Recordings;

/// <summary>A continuous stretch of recording; times in UTC.</summary>
public sealed record RecordingPieceResponse(DateTimeOffset Start, DateTimeOffset End, IReadOnlyList<RecordingSegmentResponse> Segments);
