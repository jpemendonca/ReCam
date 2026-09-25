using Recam.Server.Infrastructure.Presence;

namespace Recam.Server.Tests.Infrastructure.Presence;

public sealed class DevicePresenceTests
{
    [Fact(DisplayName = "A device stays online until its last connection closes")]
    public void Disconnect_WithTwoConnections_StaysOnlineUntilLast()
    {
        // arrange
        var presence = new DevicePresence();
        var deviceId = Guid.NewGuid();
        var firstCameOnline = presence.Connect(deviceId);
        var secondCameOnline = presence.Connect(deviceId);

        // act
        var firstWentOffline = presence.Disconnect(deviceId);
        var onlineInBetween = presence.IsOnline(deviceId);
        var lastWentOffline = presence.Disconnect(deviceId);

        // assert
        Assert.True(firstCameOnline);
        Assert.False(secondCameOnline);
        Assert.False(firstWentOffline);
        Assert.True(onlineInBetween);
        Assert.True(lastWentOffline);
        Assert.False(presence.IsOnline(deviceId));
    }
}
