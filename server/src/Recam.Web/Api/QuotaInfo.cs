namespace Recam.Web.Api;

/// <summary>How much disk all recordings may take, and how much the disk has.</summary>
public sealed record QuotaInfo(int QuotaMb, long UsedBytes, long FreeBytes)
{
    private const long BytesPerMegabyte = 1024 * 1024;

    /// <summary>The largest quota the server accepts: what recordings use plus what is free.</summary>
    public int MaxMegabytes => (int)((UsedBytes + FreeBytes) / BytesPerMegabyte);
}
