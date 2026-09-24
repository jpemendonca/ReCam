namespace Recam.Server.Domain;

public sealed class PairingToken
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(10);

    private PairingToken()
    {
        TokenHash = [];
    }

    public Guid Id { get; private set; }

    public byte[] TokenHash { get; private set; }

    public DeviceRole GrantsRole { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? UsedAt { get; private set; }

    public Guid? CreatedByDeviceId { get; private set; }

    /// <summary>
    /// Creates a token and returns its secret. Only the hash is kept, so the secret exists
    /// only in this return value.
    /// </summary>
    public static IssuedPairingToken Issue(DeviceRole grantsRole, DateTimeOffset now, Guid? createdByDeviceId = null)
    {
        var secret = SecretToken.Generate();
        var token = new PairingToken
        {
            Id = Guid.CreateVersion7(now),
            TokenHash = SecretToken.Hash(secret),
            GrantsRole = grantsRole,
            CreatedAt = now,
            ExpiresAt = now.Add(Lifetime),
            CreatedByDeviceId = createdByDeviceId,
        };
        return new IssuedPairingToken(token, secret);
    }

    public Result Consume(DateTimeOffset now)
    {
        if (UsedAt is not null)
        {
            return PairingErrors.TokenAlreadyUsed;
        }

        if (now >= ExpiresAt)
        {
            return PairingErrors.TokenExpired;
        }

        UsedAt = now;
        return Result.Success();
    }
}
