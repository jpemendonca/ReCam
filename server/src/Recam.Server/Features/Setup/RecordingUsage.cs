namespace Recam.Server.Features.Setup;

/// <summary>How much of the recordings quota is in use, for the panel.</summary>
public sealed record RecordingUsage(long UsedBytes, long QuotaBytes);
