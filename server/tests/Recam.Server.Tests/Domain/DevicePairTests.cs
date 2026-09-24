using Recam.Server.Domain;

namespace Recam.Server.Tests.Domain;

public sealed class DevicePairTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 24, 12, 0, 0, TimeSpan.Zero);

    [Fact(DisplayName = "Pairing takes the role from the token and keeps only the credential hash")]
    public void Pair_WithValidToken_CreatesDeviceWithTokenRole()
    {
        // arrange
        var token = PairingToken.Issue(DeviceRole.Camera, Now).Token;

        // act
        var result = Device.Pair(token, "Kitchen", Now);

        // assert
        var paired = result.Value;
        Assert.Equal(DeviceRole.Camera, paired.Device.Role);
        Assert.True(DeviceCredential.TryParse(paired.Credential, out var deviceId, out var secret));
        Assert.Equal(paired.Device.Id, deviceId);
        Assert.True(paired.Device.HasCredentialSecret(secret));
        Assert.False(paired.Device.HasCredentialSecret(SecretToken.Generate()));
    }

    [Fact(DisplayName = "Pairing with an expired token fails with the token error")]
    public void Pair_WithExpiredToken_ReturnsTokenExpired()
    {
        // arrange
        var token = PairingToken.Issue(DeviceRole.Camera, Now).Token;

        // act
        var result = Device.Pair(token, "Kitchen", Now.AddHours(1));

        // assert
        Assert.Equal(PairingErrors.TokenExpired, result.Error);
    }

    [Fact(DisplayName = "Pairing with a name that skipped validation is a bug")]
    public void Pair_WithInvalidName_Throws()
    {
        // arrange
        var token = PairingToken.Issue(DeviceRole.Camera, Now).Token;

        // act
        var exception = Record.Exception(() => Device.Pair(token, "", Now));

        // assert
        Assert.IsType<ArgumentException>(exception);
    }
}
