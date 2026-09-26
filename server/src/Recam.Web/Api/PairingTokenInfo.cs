namespace Recam.Web.Api;

/// <summary>A new pairing QR: its id, to ask later whether someone used it, and how long it lasts.</summary>
public sealed record PairingTokenInfo(Guid Id, string QrUri, TimeSpan ValidFor);
