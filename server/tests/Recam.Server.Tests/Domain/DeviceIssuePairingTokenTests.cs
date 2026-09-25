using Recam.Server.Domain;
using static Recam.Server.Tests.Support.ApiJson;

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

    [Theory(DisplayName = "A viewer adds cameras and other viewers")]
    [InlineData(DeviceRole.Camera)]
    [InlineData(DeviceRole.Viewer)]
    public void IssuePairingToken_ByViewer_IssuesTokenForRole(DeviceRole grantsRole)
    {
        // arrange
        var viewer = PairDevice(DeviceRole.Viewer);

        // act
        var result = viewer.IssuePairingToken(grantsRole, Now);

        // assert
        Assert.Equal(grantsRole, result.Value.Token.GrantsRole);
        Assert.Equal(viewer.Id, result.Value.Token.CreatedByDeviceId);
    }

    [Fact(DisplayName = "A camera cannot issue tokens")]
    public void IssuePairingToken_ByCamera_ReturnsIssuerCannotInvite()
    {
        // arrange
        var camera = PairDevice(DeviceRole.Camera);

        // act
        var result = camera.IssuePairingToken(DeviceRole.Camera, Now);

        // assert
        Assert.Equal(PairingErrors.IssuerCannotInvite, result.Error);
    }

    [Theory(DisplayName = "A token can never grant the owner role")]
    [InlineData(DeviceRole.Owner)]
    [InlineData(DeviceRole.Viewer)]
    public void IssuePairingToken_ForOwnerRole_ReturnsOwnerRoleNotGrantable(DeviceRole issuerRole)
    {
        // arrange
        var issuer = PairDevice(issuerRole);

        // act
        var result = issuer.IssuePairingToken(DeviceRole.Owner, Now);

        // assert
        Assert.Equal(PairingErrors.OwnerRoleNotGrantable, result.Error);
    }

    private static Device PairDevice(DeviceRole role)
    {
        var token = PairingToken.Issue(role, Now).Token;
        return Device.Pair(token, "Test device", AnyRole, Now).Value.Device;
    }
}
