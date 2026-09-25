using Recam.Server.Domain;

namespace Recam.Server.Tests.Domain;

public sealed class PairingUriTests
{
    [Fact(DisplayName = "Pairing URI carries version, token, role, fingerprint and every server URL")]
    public void Build_WithFingerprintAndTwoUrls_FollowsSpecFormat()
    {
        // arrange
        var fingerprint = new string('a', 64);
        Uri[] urls = [new("https://192.168.0.10:8443"), new("https://10.0.0.2:8443")];

        // act
        var uri = PairingUri.Build("tok-en_1", DeviceRole.Camera, fingerprint, urls);

        // assert
        Assert.Equal(
            $"recam://pair?v=1&t=tok-en_1&r=camera&f={fingerprint}&u=https%3A%2F%2F192.168.0.10%3A8443&u=https%3A%2F%2F10.0.0.2%3A8443",
            uri);
    }

    [Fact(DisplayName = "Pairing URI omits the fingerprint when there is none")]
    public void Build_WithoutFingerprint_OmitsF()
    {
        // arrange
        Uri[] urls = [new("https://cam.example.com")];

        // act
        var uri = PairingUri.Build("token", DeviceRole.Owner, null, urls);

        // assert
        Assert.Equal("recam://pair?v=1&t=token&r=owner&u=https%3A%2F%2Fcam.example.com", uri);
    }
}
