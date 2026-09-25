using Recam.Server.Domain;

namespace Recam.Server.Tests.Domain;

public sealed class DeviceIssuePairingTokenTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 24, 12, 0, 0, TimeSpan.Zero);

    [Fact(DisplayName = "The owner issues a camera token that remembers who created it")]
    public void IssuePairingToken_ByOwner_IssuesTokenForRole()
    {
        // arrange
        var owner = PairDevice(DeviceRole.Owner);

        // act
        var result = owner.IssuePairingToken(DeviceRole.Camera, Now);

        // assert
        var token = result.Value.Token;
        Assert.Equal(DeviceRole.Camera, token.GrantsRole);
        Assert.Equal(owner.Id, token.CreatedByDeviceId);
        Assert.Equal(Now.Add(PairingToken.Lifetime), token.ExpiresAt);
    }

    [Theory(DisplayName = "Devices that are not the owner cannot issue tokens")]
    [InlineData(DeviceRole.Camera)]
    [InlineData(DeviceRole.Viewer)]
    public void IssuePairingToken_ByNonOwner_ReturnsIssuerNotOwner(DeviceRole role)
    {
        // arrange
        var device = PairDevice(role);

        // act
        var result = device.IssuePairingToken(DeviceRole.Camera, Now);

        // assert
        Assert.Equal(PairingErrors.IssuerNotOwner, result.Error);
    }

    [Fact(DisplayName = "A token can never grant the owner role")]
    public void IssuePairingToken_ForOwnerRole_ReturnsOwnerRoleNotGrantable()
    {
        // arrange
        var owner = PairDevice(DeviceRole.Owner);

        // act
        var result = owner.IssuePairingToken(DeviceRole.Owner, Now);

        // assert
        Assert.Equal(PairingErrors.OwnerRoleNotGrantable, result.Error);
    }

    private static Device PairDevice(DeviceRole role)
    {
        var token = PairingToken.Issue(role, Now).Token;
        return Device.Pair(token, "Test device", Now).Value.Device;
    }
}
