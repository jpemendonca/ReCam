using Recam.Server.Domain;

namespace Recam.Server.Tests.Domain;

public sealed class PairingTokenConsumeTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 24, 12, 0, 0, TimeSpan.Zero);

    [Fact(DisplayName = "An expired token cannot be consumed")]
    public void Consume_WhenExpired_ReturnsTokenExpired()
    {
        // arrange
        var token = PairingToken.Issue(DeviceRole.Camera, Now).Token;

        // act
        var result = token.Consume(Now.Add(PairingToken.Lifetime));

        // assert
        Assert.Equal(PairingErrors.TokenExpired, result.Error);
        Assert.Null(token.UsedAt);
    }

    [Fact(DisplayName = "A token cannot be consumed twice")]
    public void Consume_WhenUsed_ReturnsTokenAlreadyUsed()
    {
        // arrange
        var token = PairingToken.Issue(DeviceRole.Camera, Now).Token;
        token.Consume(Now);

        // act
        var result = token.Consume(Now.AddMinutes(1));

        // assert
        Assert.Equal(PairingErrors.TokenAlreadyUsed, result.Error);
        Assert.Equal(Now, token.UsedAt);
    }

    [Fact(DisplayName = "A valid token is marked as used")]
    public void Consume_WhenValid_MarksUsed()
    {
        // arrange
        var token = PairingToken.Issue(DeviceRole.Camera, Now).Token;

        // act
        var result = token.Consume(Now.AddMinutes(9));

        // assert
        Assert.True(result.IsSuccess);
        Assert.Equal(Now.AddMinutes(9), token.UsedAt);
    }
}
