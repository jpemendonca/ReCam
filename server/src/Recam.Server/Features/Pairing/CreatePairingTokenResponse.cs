namespace Recam.Server.Features.Pairing;

/// <summary>The id lets the creator ask later whether someone paired with the token.</summary>
public sealed record CreatePairingTokenResponse(Guid Id, string QrUri, DateTimeOffset ExpiresAt);
