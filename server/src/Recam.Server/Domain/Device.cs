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

    /// <summary>Battery temperature in °C, the closest a phone offers to "is it overheating".</summary>
    public double? TemperatureC { get; private set; }

    public DateTimeOffset? TelemetryAt { get; private set; }

    /// <summary>"Record always": the camera publishes all the time and MediaMTX records it.</summary>
    public bool RecordingEnabled { get; private set; }

    /// <summary>How much of the picture must change for motion to count in its recordings.</summary>
    public MotionSensitivity MotionSensitivity { get; private set; } = MotionSensitivity.Medium;

    /// <summary>What the camera said about its encoder; null until it reports.</summary>
    public bool? SupportsH264 { get; private set; }

    /// <summary>MediaMTX records H.264 only; a camera that sends VP8 streams live but cannot record.</summary>
    public bool CanRecord => Role == DeviceRole.Camera && SupportsH264 != false;

    public const double MinTemperatureC = -40;
    public const double MaxTemperatureC = 120;

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
    /// The browser that typed the first-time code becomes the owner. The rules that allow it
    /// live in <see cref="FirstOpenCode"/>.
    /// </summary>
    internal static PairedDevice CreateFirstMonitor(string name, DateTimeOffset now) => CreateBrowser(name, DeviceRole.Owner, now);

    /// <summary>A browser a Monitor phone approved becomes a viewer (<see cref="BrowserLink"/>).</summary>
    internal static PairedDevice CreateLinkedBrowser(string name, DateTimeOffset now) => CreateBrowser(name, DeviceRole.Viewer, now);

    private static PairedDevice CreateBrowser(string name, DeviceRole role, DateTimeOffset now)
    {
        if (DeviceName.Validate(name).Count > 0)
        {
            throw new ArgumentException("The browser's device name must be valid.", nameof(name));
        }

        var secret = SecretToken.Generate();
        var device = new Device
        {
            Id = Guid.CreateVersion7(now),
            Name = name.Trim(),
            Role = role,
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
        if (!IsMonitor || IsRevoked)
        {
            return PairingErrors.IssuerCannotInvite;
        }

        if (grantsRole == DeviceRole.Owner)
        {
            return PairingErrors.OwnerRoleNotGrantable;
        }

        return PairingToken.Issue(grantsRole, now, Id);
    }

    public Result ReportTelemetry(int batteryLevel, bool isCharging, double? temperatureC, DateTimeOffset now)
    {
        if (Role != DeviceRole.Camera)
        {
            return DeviceErrors.NotACamera;
        }

        if (batteryLevel is < 0 or > 100)
        {
            return DeviceErrors.InvalidBatteryLevel;
        }

        if (temperatureC is < MinTemperatureC or > MaxTemperatureC || (temperatureC is { } value && double.IsNaN(value)))
        {
            return DeviceErrors.InvalidTemperature;
        }

        BatteryLevel = batteryLevel;
        IsCharging = isCharging;
        TemperatureC = temperatureC;
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

    /// <summary>
    /// A Monitor takes another device off the server. A device leaves by itself through
    /// <see cref="Revoke"/> ("Reset app"), never through here.
    /// </summary>
    public Result RevokeBy(Device requester, DateTimeOffset now)
    {
        if (!requester.IsMonitor || requester.IsRevoked)
        {
            return DeviceErrors.NotAMonitor;
        }

        if (requester.Id == Id)
        {
            return DeviceErrors.CannotRemoveItself;
        }

        if (IsRevoked)
        {
            return DeviceErrors.NotFound;
        }

        Revoke(now);
        return Result.Success();
    }

    /// <summary>
    /// Takes the device off the server: its credential stops working. Revoking twice keeps the
    /// first time.
    /// </summary>
    public void Revoke(DateTimeOffset now) => RevokedAt ??= now;

    /// <summary>
    /// Turns "record always" on or off. Only an active Monitor may do it, only for a camera, and
    /// never on for a camera without H.264.
    /// </summary>
    public Result SetRecording(Device requester, bool enabled)
    {
        if (!requester.IsMonitor || requester.IsRevoked)
        {
            return DeviceErrors.NotAMonitor;
        }

        if (Role != DeviceRole.Camera || IsRevoked)
        {
            return MediaErrors.CameraNotFound;
        }

        if (enabled && !CanRecord)
        {
            return MediaErrors.RecordingNeedsH264;
        }

        RecordingEnabled = enabled;
        return Result.Success();
    }

    /// <summary>A Monitor sets how sensitive this camera's motion detection is.</summary>
    public Result SetMotionSensitivity(Device requester, MotionSensitivity sensitivity)
    {
        if (!requester.IsMonitor || requester.IsRevoked)
        {
            return DeviceErrors.NotAMonitor;
        }

        if (Role != DeviceRole.Camera || IsRevoked)
        {
            return MediaErrors.CameraNotFound;
        }

        MotionSensitivity = sensitivity;
        return Result.Success();
    }

    /// <summary>A camera that turns out not to encode H.264 cannot keep recording.</summary>
    public void ReportVideoCodecs(bool supportsH264)
    {
        SupportsH264 = supportsH264;
        if (!supportsH264)
        {
            RecordingEnabled = false;
        }
    }

    /// <summary>Owners and viewers are the phones the app calls Monitors.</summary>
    public bool IsMonitor => Role is DeviceRole.Owner or DeviceRole.Viewer;

    public void MarkSeen(DateTimeOffset now) => LastSeenAt = now;

    public CameraStatus ToCameraStatus(bool online, bool publishing) =>
        new(Id, Name, online, publishing, BatteryLevel, IsCharging, TemperatureC, TelemetryAt, RecordingEnabled, CanRecord);

    public bool HasCredentialSecret(string secret) =>
        CryptographicOperations.FixedTimeEquals(SecretToken.Hash(secret), CredentialHash);
}
