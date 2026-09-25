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

    [Fact(DisplayName = "The creator sees whether the token was used")]
    public void UsageFor_Creator_TellsWhetherUsed()
    {
        // arrange
        var creatorId = Guid.NewGuid();
        var token = PairingToken.Issue(DeviceRole.Camera, Now, creatorId).Token;
        var before = token.UsageFor(creatorId).Value;
        token.Consume(Now, [DeviceRole.Camera]);

        // act
        var after = token.UsageFor(creatorId);

        // assert
        Assert.False(before);
        Assert.True(after.Value);
    }

    [Fact(DisplayName = "Any other device is told the token does not exist")]
    public void UsageFor_OtherDevice_ReturnsTokenNotFound()
    {
        // arrange
        var token = PairingToken.Issue(DeviceRole.Camera, Now, Guid.NewGuid()).Token;

        // act
        var usage = token.UsageFor(Guid.NewGuid());

        // assert
        Assert.Equal(PairingErrors.TokenNotFound, usage.Error);
    }
}
