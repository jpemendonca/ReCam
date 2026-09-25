namespace Recam.Server.Features.Pairing;

public sealed record CreatePairingTokenResponse(string QrUri, DateTimeOffset ExpiresAt);
