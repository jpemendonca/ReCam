namespace Recam.Server.Domain;

/// <summary>One recorded file of a camera: MediaMTX starts a new one every 60 s.</summary>
public sealed record RecordingSegment(Guid CameraId, string FileName, DateTimeOffset StartsAt, long Bytes);
