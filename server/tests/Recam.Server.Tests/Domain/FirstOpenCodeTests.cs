using Recam.Server.Domain;

namespace Recam.Server.Tests.Domain;

public sealed class FirstOpenCodeTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 26, 12, 0, 0, TimeSpan.Zero);

    [Fact(DisplayName = "The code has eight characters without look-alikes, shown in two halves")]
    public void Issue_ShowsTwoHalvesWithoutLookAlikes()
    {
        // arrange
        var code = FirstOpenCode.Issue();

        // act
        var display = code.Display;

        // assert
        Assert.Matches("^[A-HJKMNP-Z2-9]{4}-[A-HJKMNP-Z2-9]{4}$", display);
    }

    [Theory(DisplayName = "The right code makes the browser the owner, whatever the case, spaces or dash")]
    [InlineData("{0}")]
    [InlineData(" {0} ")]
    [InlineData("LOWER")]
    [InlineData("NODASH")]
    public void Open_RightCode_CreatesOwner(string format)
    {
        // arrange
        var code = FirstOpenCode.Issue();
        var typed = format switch
        {
            "LOWER" => code.Display.ToLowerInvariant(),
            "NODASH" => code.Display.Replace("-", string.Empty, StringComparison.Ordinal),
            _ => string.Format(System.Globalization.CultureInfo.InvariantCulture, format, code.Display),
        };

        // act
        var opened = code.Open(typed, "Navegador · Chrome no Windows", serverHasMonitor: false, Now);

        // assert
        Assert.True(opened.IsSuccess);
        Assert.Equal(DeviceRole.Owner, opened.Value.Device.Role);
        Assert.Equal("Navegador · Chrome no Windows", opened.Value.Device.Name);
        Assert.True(opened.Value.Device.HasCredentialSecret(opened.Value.Credential.Split('.')[1]));
    }

    [Fact(DisplayName = "A wrong code is refused")]
    public void Open_WrongCode_ReturnsWrongCode()
    {
        // arrange
        var code = FirstOpenCode.Issue();

        // act
        var opened = code.Open("AAAA-AAAA", "Browser", serverHasMonitor: false, Now);

        // assert
        Assert.Equal(SetupErrors.WrongCode, opened.Error);
        Assert.False(code.IsSpent);
    }

    [Fact(DisplayName = "After five wrong tries the code is spent, even for the right one")]
    public void Open_AfterFiveWrongTries_IsSpent()
    {
        // arrange
        var code = FirstOpenCode.Issue();
        for (var attempt = 0; attempt < FirstOpenCode.MaxAttempts; attempt++)
        {
            code.Open("AAAA-AAAA", "Browser", serverHasMonitor: false, Now);
        }

        // act
        var opened = code.Open(code.Display, "Browser", serverHasMonitor: false, Now);

        // assert
        Assert.True(code.IsSpent);
        Assert.Equal(SetupErrors.WrongCode, opened.Error);
    }

    [Fact(DisplayName = "With a Monitor on the server the code opens nothing, and that is not a wrong try")]
    public void Open_ServerHasMonitor_RefusesWithoutCounting()
    {
        // arrange
        var code = FirstOpenCode.Issue();

        // act
        var results = Enumerable.Range(0, FirstOpenCode.MaxAttempts)
            .Select(_ => code.Open(code.Display, "Browser", serverHasMonitor: true, Now))
            .ToList();

        // assert
        Assert.All(results, result => Assert.Equal(SetupErrors.AlreadyHasMonitor, result.Error));
        Assert.False(code.IsSpent);
    }
}
