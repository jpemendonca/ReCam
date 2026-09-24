using Recam.Server.Domain;

namespace Recam.Server.Tests.Domain;

public sealed class PairingTokenTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 24, 12, 0, 0, TimeSpan.Zero);

    [Fact(DisplayName = "Issuing a token stores only the hash of its secret")]
    public void Issue_WithRole_StoresHashOfSecret()
    {
        // arrange
        const DeviceRole role = DeviceRole.Camera;

        // act
        var issued = PairingToken.Issue(role, Now);

        // assert
        Assert.Equal(SecretToken.Hash(issued.Secret), issued.Token.TokenHash);
        Assert.Equal(role, issued.Token.GrantsRole);
        Assert.Equal(Now.Add(PairingToken.Lifetime), issued.Token.ExpiresAt);
        Assert.Null(issued.Token.UsedAt);
    }

    [Fact(DisplayName = "Each issued token has a different 43-character secret")]
    public void Issue_Twice_GeneratesDifferentSecrets()
    {
        // arrange
        var first = PairingToken.Issue(DeviceRole.Owner, Now);

        // act
        var second = PairingToken.Issue(DeviceRole.Owner, Now);

        // assert
        Assert.NotEqual(first.Secret, second.Secret);
        Assert.Equal(43, second.Secret.Length);
    }
}
