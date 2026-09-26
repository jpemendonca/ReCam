using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Recam.Server.Domain;
using Recam.Server.Features.Setup;
using Recam.Server.Tests.Support;

namespace Recam.Server.Tests.Features.Setup;

public sealed class ResetOwnerCommandTests
{
    [Fact(DisplayName = "reset-owner removes every Monitor, keeps the cameras, and a first-time code comes back")]
    public async Task ResetOwner_WithOwner_RevokesAndCreatesSetupToken()
    {
        // arrange
        using var factory = new RecamApiFactory();
        var owner = await factory.PairDeviceAsync(DeviceRole.Owner);
        var viewer = await factory.PairDeviceAsync(DeviceRole.Viewer);
        var camera = await factory.PairDeviceAsync(DeviceRole.Camera);
        var firstOpen = factory.Services.GetRequiredService<FirstOpen>();
        var openBefore = await firstOpen.IsOpenAsync(TestContext.Current.CancellationToken);

        // act
        int removed;
        await using (var database = await factory.CreateDatabaseAsync())
        {
            removed = await ResetOwnerCommand.RevokeMonitorsAsync(database, factory.Time.GetUtcNow(), TestContext.Current.CancellationToken);
        }

        // assert
        Assert.Equal(2, removed);
        Assert.False(openBefore);
        Assert.True(await firstOpen.IsOpenAsync(TestContext.Current.CancellationToken));
        await using var check = await factory.CreateDatabaseAsync();
        var devices = await check.Devices.ToDictionaryAsync(device => device.Id, TestContext.Current.CancellationToken);
        Assert.True(devices[owner.DeviceId].IsRevoked);
        Assert.True(devices[viewer.DeviceId].IsRevoked);
        Assert.False(devices[camera.DeviceId].IsRevoked);
        Assert.Matches("^[A-Z2-9]{4}-[A-Z2-9]{4}$", await factory.FirstOpenCodeAsync());
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
