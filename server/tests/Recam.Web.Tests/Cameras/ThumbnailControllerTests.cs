using Recam.Web.Api;
using Recam.Web.Cameras;
using Recam.Web.Tests.Support;

namespace Recam.Web.Tests.Cameras;

public sealed class ThumbnailControllerTests
{
    private readonly FakeRecamApi _api = new();

    [Fact(DisplayName = "Following the list asks again only when a camera came, went or changed what it does")]
    public async Task Follow_OnlyOnChange_AsksAgain()
    {
        // arrange
        var camera = Support.Cameras.Make("Porta");
        var controller = new ThumbnailController(_api);
        await controller.RefreshAsync([camera], CancellationToken.None);

        // act
        await controller.FollowAsync([camera with { BatteryLevel = 50 }], CancellationToken.None);
        var afterBattery = _api.ThumbnailsAsked[camera.Id];
        await controller.FollowAsync([camera with { Publishing = true }], CancellationToken.None);

        // assert
        Assert.Equal(1, afterBattery);
        Assert.Equal(2, _api.ThumbnailsAsked[camera.Id]);
    }

    [Fact(DisplayName = "A picture stays when asking fails, and goes with its camera")]
    public async Task Refresh_FailureKeeps_RemovedCameraDrops()
    {
        // arrange
        var camera = Support.Cameras.Make("Porta");
        var picture = new ThumbnailInfo("data:image/jpeg;base64,AAAA", null);
        _api.Thumbnails[camera.Id] = picture;
        var controller = new ThumbnailController(_api);
        await controller.RefreshAsync([camera], CancellationToken.None);

        // act
        _api.Offline = true;
        await controller.RefreshAsync([camera], CancellationToken.None);
        var afterFailure = controller.Of(camera.Id);
        _api.Offline = false;
        await controller.RefreshAsync([], CancellationToken.None);

        // assert
        Assert.Equal(picture, afterFailure);
        Assert.Null(controller.Of(camera.Id));
    }
}
