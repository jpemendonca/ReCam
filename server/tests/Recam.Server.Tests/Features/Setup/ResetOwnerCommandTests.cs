using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Recam.Server.Domain;
using Recam.Server.Features.Setup;
using Recam.Server.Tests.Support;

namespace Recam.Server.Tests.Features.Setup;

public sealed class ResetOwnerCommandTests
{
    private static readonly Uri SetupUri = new("/setup", UriKind.Relative);

    [Fact(DisplayName = "reset-owner removes every Monitor, keeps the cameras, and the first-Monitor QR comes back")]
    public async Task ResetOwner_WithOwner_RevokesAndCreatesSetupToken()
    {
        // arrange
        using var factory = new RecamApiFactory();
        var owner = await factory.PairDeviceAsync(DeviceRole.Owner);
        var viewer = await factory.PairDeviceAsync(DeviceRole.Viewer);
        var camera = await factory.PairDeviceAsync(DeviceRole.Camera);
        var ownerSetup = factory.Services.GetRequiredService<OwnerSetup>();
        var before = await ownerSetup.EnsureTokenAsync(TestContext.Current.CancellationToken);

        // act
        int removed;
        await using (var database = await factory.CreateDatabaseAsync())
        {
            removed = await ResetOwnerCommand.RevokeMonitorsAsync(database, factory.Time.GetUtcNow(), TestContext.Current.CancellationToken);
        }

        // assert
        Assert.Equal(2, removed);
        Assert.IsType<OwnerSetupStatus.Configured>(before);
        Assert.IsType<OwnerSetupStatus.Pending>(await ownerSetup.EnsureTokenAsync(TestContext.Current.CancellationToken));
        await using var check = await factory.CreateDatabaseAsync();
        var devices = await check.Devices.ToDictionaryAsync(device => device.Id, TestContext.Current.CancellationToken);
        Assert.True(devices[owner.DeviceId].IsRevoked);
        Assert.True(devices[viewer.DeviceId].IsRevoked);
        Assert.False(devices[camera.DeviceId].IsRevoked);
        using var client = factory.CreateClient();
        var page = await client.GetStringAsync(SetupUri, TestContext.Current.CancellationToken);
        Assert.Contains("<svg", page, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "reset-owner on a server with no Monitor changes nothing")]
    public async Task ResetOwner_WithoutMonitors_RemovesNothing()
    {
        // arrange
        using var factory = new RecamApiFactory();
        var camera = await factory.PairDeviceAsync(DeviceRole.Camera);
        await using var database = await factory.CreateDatabaseAsync();

        // act
        var removed = await ResetOwnerCommand.RevokeMonitorsAsync(database, factory.Time.GetUtcNow(), TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(0, removed);
        Assert.False((await database.Devices.SingleAsync(device => device.Id == camera.DeviceId, TestContext.Current.CancellationToken)).IsRevoked);
    }
}
