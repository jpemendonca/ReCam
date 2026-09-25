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
        var firstCameOnline = presence.Connect(deviceId, isCamera: false);
        var secondCameOnline = presence.Connect(deviceId, isCamera: false);

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

    [Fact(DisplayName = "The watcher count follows the last value set and drops at zero")]
    public void SetWatchers_ThenZero_ForgetsTheCount()
    {
        // arrange
        var presence = new DevicePresence();
        var cameraId = Guid.NewGuid();
        presence.SetWatchers(cameraId, 2);
        var whileWatched = presence.Watchers(cameraId);

        // act
        presence.SetWatchers(cameraId, 0);

        // assert
        Assert.Equal(2, whileWatched);
        Assert.Equal(0, presence.Watchers(cameraId));
    }

    [Fact(DisplayName = "The counters follow cameras, publishing and views, and forget a camera that goes offline")]
    public void Counters_CameraGoesOffline_DropCameraAndPublishing()
    {
        // arrange
        var presence = new DevicePresence();
        var cameraId = Guid.NewGuid();
        var otherCameraId = Guid.NewGuid();
        presence.Connect(cameraId, isCamera: true);
        presence.Connect(otherCameraId, isCamera: true);
        presence.Connect(Guid.NewGuid(), isCamera: false);
        presence.SetPublishing(cameraId, true);
        presence.SetWatchers(cameraId, 2);
        presence.SetWatchers(otherCameraId, 1);
        var before = (presence.OnlineCameras, presence.PublishingCameras, presence.ActiveViews);

        // act
        presence.Disconnect(cameraId);

        // assert
        Assert.Equal((2, 1, 3), before);
        Assert.Equal(1, presence.OnlineCameras);
        Assert.Equal(0, presence.PublishingCameras);
    }
}
