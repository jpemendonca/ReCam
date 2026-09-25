namespace Recam.Server.Features.Recordings;

public sealed record QuotaResponse(int QuotaMb, long UsedBytes, long FreeBytes);
