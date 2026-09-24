namespace Recam.Server.Domain;

public sealed record IssuedPairingToken(PairingToken Token, string Secret);
