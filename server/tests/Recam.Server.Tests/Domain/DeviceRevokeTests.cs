using Recam.Server.Domain;
using static Recam.Server.Tests.Support.ApiJson;

namespace Recam.Server.Tests.Domain;

public sealed class DeviceRevokeTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 25, 12, 0, 0, TimeSpan.Zero);

    private static Device Paired(DeviceRole role) =>
        Device.Pair(PairingToken.Issue(role, Now).Token, "Test", AnyRole, Now).Value.Device;

    [Fact(DisplayName = "Revoking twice keeps the first revocation time")]
    public void Revoke_Twice_KeepsFirstTime()
    {
        // arrange
        var camera = Paired(DeviceRole.Camera);
        camera.Revoke(Now);

        // act
        camera.Revoke(Now.AddMinutes(5));

        // assert
        Assert.True(camera.IsRevoked);
        Assert.Equal(Now, camera.RevokedAt);
    }

    [Fact(DisplayName = "A revoked Monitor can no longer add devices")]
    public void IssuePairingToken_AfterRevoke_ReturnsIssuerCannotInvite()
    {
        // arrange
        var viewer = Paired(DeviceRole.Viewer);
        viewer.Revoke(Now);

        // act
        var result = viewer.IssuePairingToken(DeviceRole.Camera, Now);

        // assert
        Assert.Equal(PairingErrors.IssuerCannotInvite, result.Error);
    }

    [Theory(DisplayName = "A Monitor removes a camera or another Monitor")]
    [InlineData(DeviceRole.Camera)]
    [InlineData(DeviceRole.Viewer)]
    public void RevokeBy_Monitor_RevokesTheTarget(DeviceRole targetRole)
    {
        // arrange
        var monitor = Paired(DeviceRole.Viewer);
        var target = Paired(targetRole);

        // act
        var result = target.RevokeBy(monitor, Now);

        // assert
        Assert.True(result.IsSuccess);
        Assert.Equal(Now, target.RevokedAt);
    }

    [Fact(DisplayName = "A camera cannot remove devices")]
    public void RevokeBy_Camera_ReturnsNotAMonitor()
    {
        // arrange
        var camera = Paired(DeviceRole.Camera);
        var other = Paired(DeviceRole.Camera);

        // act
        var result = other.RevokeBy(camera, Now);

        // assert
        Assert.Equal(DeviceErrors.NotAMonitor, result.Error);
        Assert.False(other.IsRevoked);
    }

    [Fact(DisplayName = "A Monitor does not remove itself from the list; that is Reset app")]
    public void RevokeBy_Itself_ReturnsCannotRemoveItself()
    {
        // arrange
        var owner = Paired(DeviceRole.Owner);

        // act
        var result = owner.RevokeBy(owner, Now);

        // assert
        Assert.Equal(DeviceErrors.CannotRemoveItself, result.Error);
        Assert.False(owner.IsRevoked);
    }

    [Fact(DisplayName = "A device already removed is not found")]
    public void RevokeBy_AlreadyRevoked_ReturnsNotFound()
    {
        // arrange
        var owner = Paired(DeviceRole.Owner);
        var camera = Paired(DeviceRole.Camera);
        camera.Revoke(Now);

        // act
        var result = camera.RevokeBy(owner, Now.AddMinutes(1));

        // assert
        Assert.Equal(DeviceErrors.NotFound, result.Error);
        Assert.Equal(Now, camera.RevokedAt);
    }
}
