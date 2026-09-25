using System.Security.Cryptography;

namespace Recam.Server.Domain;

public sealed class Device
{
    private Device()
    {
        Name = string.Empty;
        CredentialHash = [];
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; }

    public DeviceRole Role { get; private set; }

    public byte[] CredentialHash { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? LastSeenAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public bool IsRevoked => RevokedAt is not null;

    /// <summary>
    /// Consumes the token and creates the device with the role the token grants. The returned
    /// credential is the only copy of the secret; the device keeps its hash.
    /// </summary>
    public static Result<PairedDevice> Pair(PairingToken token, string name, DateTimeOffset now)
    {
        if (DeviceName.Validate(name).Count > 0)
        {
            throw new ArgumentException("Device name must be validated before pairing.", nameof(name));
        }

        var consumed = token.Consume(now);
        if (consumed.IsFailure)
        {
            return consumed.Error;
        }

        var secret = SecretToken.Generate();
        var device = new Device
        {
            Id = Guid.CreateVersion7(now),
            Name = name.Trim(),
            Role = token.GrantsRole,
            CredentialHash = SecretToken.Hash(secret),
            CreatedAt = now,
        };
        return new PairedDevice(device, DeviceCredential.Format(device.Id, secret));
    }

    /// <summary>
    /// Creates a pairing token on behalf of this device. Only an active owner may do it, and
    /// never for the owner role: the server has a single owner.
    /// </summary>
    public Result<IssuedPairingToken> IssuePairingToken(DeviceRole grantsRole, DateTimeOffset now)
    {
        if (Role != DeviceRole.Owner || IsRevoked)
        {
            return PairingErrors.IssuerNotOwner;
        }

        if (grantsRole == DeviceRole.Owner)
        {
            return PairingErrors.OwnerRoleNotGrantable;
        }

        return PairingToken.Issue(grantsRole, now, Id);
    }

    public bool HasCredentialSecret(string secret) =>
        CryptographicOperations.FixedTimeEquals(SecretToken.Hash(secret), CredentialHash);
}
