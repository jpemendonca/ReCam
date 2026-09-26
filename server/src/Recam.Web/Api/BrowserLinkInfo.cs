namespace Recam.Web.Api;

/// <summary>A "Connect browser" QR code; <see cref="Claim"/> never leaves this browser.</summary>
public sealed record BrowserLinkInfo(Guid Id, string QrUri, string Claim, TimeSpan ValidFor);

public enum ClaimOutcome
{
    /// <summary>A Monitor phone approved; this browser now has its cookie.</summary>
    Claimed,

    /// <summary>No phone approved yet.</summary>
    Waiting,

    /// <summary>The link expired or was used; a new QR is needed.</summary>
    Gone,
}
