namespace Recam.Web.Api;

/// <summary>A continuous stretch of recording (UTC), with the files that make it up.</summary>
public sealed record RecordingPieceInfo(DateTimeOffset Start, DateTimeOffset End, IReadOnlyList<RecordingSegmentInfo> Segments);

/// <summary>One playable file; <see cref="Url"/> is relative to the server.</summary>
public sealed record RecordingSegmentInfo(DateTimeOffset Start, DateTimeOffset End, string Url);
