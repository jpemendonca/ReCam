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
}
