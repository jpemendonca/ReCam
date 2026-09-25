namespace Recam.Server.Domain;

public static class PairingErrors
{
    public static readonly DomainError TokenExpired =
        new("pairing.token_expired", "The pairing token has expired.", ErrorType.Unauthorized);

    public static readonly DomainError TokenAlreadyUsed =
        new("pairing.token_already_used", "The pairing token was already used.", ErrorType.Unauthorized);

    public static readonly DomainError WrongRole =
        new("pairing.wrong_role", "This pairing code is for another kind of device. Use it in the other tab.", ErrorType.Conflict);

    /// <summary>
    /// What clients see for any token problem. Unknown, expired and used tokens look the same,
    /// so a caller cannot probe which tokens exist.
    /// </summary>
    public static readonly DomainError InvalidToken =
        new("pairing.invalid_token", "The pairing token is invalid or expired. Generate a new QR code.", ErrorType.Unauthorized);

    public static readonly DomainError TokenNotFound =
        new("pairing.token_not_found", "There is no such pairing token.", ErrorType.NotFound);

    public static readonly DomainError IssuerCannotInvite =
        new("pairing.issuer_cannot_invite", "Only a phone that watches can create pairing tokens.", ErrorType.Forbidden);

    public static readonly DomainError OwnerRoleNotGrantable =
        new("pairing.owner_role_not_grantable", "A pairing token cannot grant the owner role.", ErrorType.Forbidden);
}
