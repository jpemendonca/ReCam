using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Recam.Server.Domain;
using Recam.Server.Features.Setup;
using Recam.Server.Tests.Support;

namespace Recam.Server.Tests.Features.Setup;

public sealed class OwnerSetupTests
{
    [Fact(DisplayName = "Without an owner, setup issues an owner pairing token")]
    public async Task Setup_WithoutOwner_CreatesOwnerToken()
    {
        // arrange
        using var factory = new RecamApiFactory();
        var ownerSetup = factory.Services.GetRequiredService<OwnerSetup>();

        // act
        var status = await ownerSetup.EnsureTokenAsync(TestContext.Current.CancellationToken);

        // assert
        var pending = Assert.IsType<OwnerSetupStatus.Pending>(status);
        Assert.StartsWith("recam://pair?v=1&t=", pending.PairingUri, StringComparison.Ordinal);
        Assert.Equal(factory.Time.GetUtcNow().Add(PairingToken.Lifetime), pending.ExpiresAt);
        await using var database = await factory.CreateDatabaseAsync();
        var token = Assert.Single(await database.PairingTokens.ToListAsync(TestContext.Current.CancellationToken));
        Assert.Equal(DeviceRole.Owner, token.GrantsRole);
    }

    [Fact(DisplayName = "The QR in the log tells the person to choose Watch; the phone becomes a Monitor")]
    public async Task Setup_WithoutOwner_LogsQrThatSpeaksOfWatchAndMonitor()
    {
        // arrange
        using var factory = new RecamApiFactory();
        var ownerSetup = factory.Services.GetRequiredService<OwnerSetup>();

        // act
        var status = await ownerSetup.EnsureTokenAsync(TestContext.Current.CancellationToken);

        // assert
        var pending = Assert.IsType<OwnerSetupStatus.Pending>(status);
        var message = Assert.Single(factory.Logs.Messages, message => message.Contains(pending.PairingUri, StringComparison.Ordinal));
        Assert.Contains("No Monitor paired yet", message, StringComparison.Ordinal);
        Assert.Contains("choose Watch", message, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Calling setup again before expiry keeps the same token")]
    public async Task Setup_BeforeExpiry_ReusesToken()
    {
        // arrange
        using var factory = new RecamApiFactory();
        var ownerSetup = factory.Services.GetRequiredService<OwnerSetup>();
        var first = await ownerSetup.EnsureTokenAsync(TestContext.Current.CancellationToken);
        factory.Time.Advance(TimeSpan.FromMinutes(5));

        // act
        var second = await ownerSetup.EnsureTokenAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(first, second);
    }

    [Fact(DisplayName = "An expired owner token is replaced and the old one deleted")]
    public async Task Setup_WhenTokenExpires_CreatesNewToken()
    {
        // arrange
        using var factory = new RecamApiFactory();
        var ownerSetup = factory.Services.GetRequiredService<OwnerSetup>();
        var first = Assert.IsType<OwnerSetupStatus.Pending>(await ownerSetup.EnsureTokenAsync(TestContext.Current.CancellationToken));
        factory.Time.Advance(PairingToken.Lifetime + TimeSpan.FromSeconds(1));

        // act
        var second = Assert.IsType<OwnerSetupStatus.Pending>(await ownerSetup.EnsureTokenAsync(TestContext.Current.CancellationToken));

        // assert
        Assert.NotEqual(first.PairingUri, second.PairingUri);
        await using var database = await factory.CreateDatabaseAsync();
        var remaining = Assert.Single(await database.PairingTokens.ToListAsync(TestContext.Current.CancellationToken));
        Assert.Equal(second.ExpiresAt, remaining.ExpiresAt);
    }
}
