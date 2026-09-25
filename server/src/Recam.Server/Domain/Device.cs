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

    public int? BatteryLevel { get; private set; }

    public bool? IsCharging { get; private set; }

    public DateTimeOffset? TelemetryAt { get; private set; }

    public bool IsRevoked => RevokedAt is not null;

    /// <summary>
    /// Consumes the token and creates the device with the role the token grants. The returned
    /// credential is the only copy of the secret; the device keeps its hash.
    /// </summary>
    public static Result<PairedDevice> Pair(
        PairingToken token, string name, IReadOnlyCollection<DeviceRole> acceptedRoles, DateTimeOffset now)
    {
        if (DeviceName.Validate(name).Count > 0)
        {
            throw new ArgumentException("Device name must be validated before pairing.", nameof(name));
        }

        var consumed = token.Consume(now, acceptedRoles);
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
    /// Creates a pairing token on behalf of this device. Any active phone that watches (owner or
    /// viewer) may add cameras and other viewers, but never an owner: the server has a single one.
    /// </summary>
    public Result<IssuedPairingToken> IssuePairingToken(DeviceRole grantsRole, DateTimeOffset now)
    {
        if (Role is not (DeviceRole.Owner or DeviceRole.Viewer) || IsRevoked)
        {
            return PairingErrors.IssuerCannotInvite;
        }

        if (grantsRole == DeviceRole.Owner)
        {
            return PairingErrors.OwnerRoleNotGrantable;
        }

        return PairingToken.Issue(grantsRole, now, Id);
    }

    public Result ReportTelemetry(int batteryLevel, bool isCharging, DateTimeOffset now)
    {
        if (Role != DeviceRole.Camera)
        {
            return DeviceErrors.NotACamera;
        }

        if (batteryLevel is < 0 or > 100)
        {
            return DeviceErrors.InvalidBatteryLevel;
        }

        BatteryLevel = batteryLevel;
        IsCharging = isCharging;
        TelemetryAt = now;
        LastSeenAt = now;
        return Result.Success();
    }

    /// <summary>
    /// Checks that a viewer may switch this camera's torch. The torch belongs to the camera's
    /// video track, so it can only be switched while the camera publishes.
    /// </summary>
    public Result AcceptTorchCommand(bool publishing)
    {
        if (Role != DeviceRole.Camera || IsRevoked)
        {
            return MediaErrors.CameraNotFound;
        }

        if (!publishing)
        {
            return MediaErrors.CameraNotPublishing;
        }

        return Result.Success();
    }

    public void MarkSeen(DateTimeOffset now) => LastSeenAt = now;

    public CameraStatus ToCameraStatus(bool online, bool publishing) =>
        new(Id, Name, online, publishing, BatteryLevel, IsCharging, TelemetryAt);

    public bool HasCredentialSecret(string secret) =>
        CryptographicOperations.FixedTimeEquals(SecretToken.Hash(secret), CredentialHash);
}
