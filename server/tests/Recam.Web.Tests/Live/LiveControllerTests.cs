using Microsoft.AspNetCore.Components;
using Recam.Web.Live;
using Recam.Web.Tests.Support;

namespace Recam.Web.Tests.Live;

public sealed class LiveControllerTests : IAsyncDisposable
{
    private readonly FakeDeviceHub _hub = new();
    private readonly FakeLiveVideo _video = new();
    private readonly Guid _cameraId = Guid.NewGuid();

    public ValueTask DisposeAsync() => _video.DisposeAsync();

    [Fact(DisplayName = "Opening takes a watch lease and retries until the camera publishes")]
    public async Task Open_CameraStillOpening_RetriesThenPlays()
    {
        // arrange
        _video.RefusalsBeforePlaying = 3;
        await using var controller = new LiveController(_hub, _video) { RetryInterval = TimeSpan.Zero };

        // act
        await controller.OpenAsync(_cameraId, default(ElementReference));

        // assert
        Assert.Equal(LiveState.Playing, controller.State);
        Assert.Equal(4, _video.Starts);
        Assert.Equal($"WatchCamera {_cameraId}", Assert.Single(_hub.Calls));
    }

    [Fact(DisplayName = "After too many refusals the view fails, and retrying plays")]
    public async Task Open_NeverPublishes_FailsThenRetryPlays()
    {
        // arrange
        _video.RefusalsBeforePlaying = LiveController.MaxAttempts;
        await using var controller = new LiveController(_hub, _video) { RetryInterval = TimeSpan.Zero };
        await controller.OpenAsync(_cameraId, default(ElementReference));
        var failed = controller.State;

        // act
        await controller.RetryAsync();

        // assert
        Assert.Equal(LiveState.Failed, failed);
        Assert.Equal(LiveState.Playing, controller.State);
    }

    [Fact(DisplayName = "Closing the view stops the video and gives the lease back")]
    public async Task Dispose_StopsVideoAndUnwatches()
    {
        // arrange
        var controller = new LiveController(_hub, _video) { RetryInterval = TimeSpan.Zero };
        await controller.OpenAsync(_cameraId, default(ElementReference));

        // act
        await controller.DisposeAsync();

        // assert
        Assert.True(_video.Disposed);
        Assert.Equal([$"WatchCamera {_cameraId}", $"UnwatchCamera {_cameraId}"], _hub.Calls);
    }

    [Fact(DisplayName = "The torch shows what the camera reports, and a refusal is reported")]
    public async Task Torch_FollowsCameraAndReportsRefusal()
    {
        // arrange
        await using var controller = new LiveController(_hub, _video) { RetryInterval = TimeSpan.Zero };
        await controller.OpenAsync(_cameraId, default(ElementReference));
        var accepted = await controller.SetTorchAsync(true);
        var beforeReport = controller.TorchOn;
        _hub.TorchAccepted = false;

        // act
        _hub.SendTorch(_cameraId, true);
        _hub.SendTorch(Guid.NewGuid(), false);
        var refused = await controller.SetTorchAsync(false);

        // assert
        Assert.True(accepted);
        Assert.False(beforeReport);
        Assert.True(controller.TorchOn);
        Assert.False(refused);
    }

    [Fact(DisplayName = "A dropped stream reconnects by itself")]
    public async Task Ended_Reconnects()
    {
        // arrange
        await using var controller = new LiveController(_hub, _video) { RetryInterval = TimeSpan.Zero };
        await controller.OpenAsync(_cameraId, default(ElementReference));

        // act
        _video.End();

        // assert
        Assert.Equal(1, _video.Stops);
        Assert.Equal(2, _video.Starts);
        Assert.Equal(LiveState.Playing, controller.State);
    }
}
