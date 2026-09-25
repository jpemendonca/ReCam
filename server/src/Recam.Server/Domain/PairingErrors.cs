namespace Recam.Server.Domain;

public static class PairingErrors
{
    public static readonly DomainError TokenExpired =
        new("pairing.token_expired", "The pairing token has expired.", ErrorType.Unauthorized);

    public static readonly DomainError TokenAlreadyUsed =
        new("pairing.token_already_used", "The pairing token was already used.", ErrorType.Unauthorized);

    /// <summary>
    /// What clients see for any token problem. Unknown, expired and used tokens look the same,
    /// so a caller cannot probe which tokens exist.
    /// </summary>
    public static readonly DomainError InvalidToken =
        new("pairing.invalid_token", "The pairing token is invalid or expired. Generate a new QR code.", ErrorType.Unauthorized);

    public static readonly DomainError IssuerNotOwner =
        new("pairing.issuer_not_owner", "Only the owner can create pairing tokens.", ErrorType.Forbidden);

    public static readonly DomainError OwnerRoleNotGrantable =
        new("pairing.owner_role_not_grantable", "A pairing token cannot grant the owner role.", ErrorType.Forbidden);
}
