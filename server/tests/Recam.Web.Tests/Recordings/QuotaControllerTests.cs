using Recam.Web.Api;
using Recam.Web.Recordings;
using Recam.Web.Tests.Support;

namespace Recam.Web.Tests.Recordings;

public sealed class QuotaControllerTests
{
    private readonly FakeRecamApi _api = new()
    {
        Me = new MeInfo(Guid.NewGuid(), "Navegador", "owner"),
        Quota = new QuotaInfo(2048, 1024L * 1024 * 1024, 2L * 1024 * 1024 * 1024),
    };

    [Fact(DisplayName = "The slider starts at the saved space and stops at the minimum and at what the disk allows")]
    public async Task Select_OutOfRange_IsClamped()
    {
        // arrange
        var controller = new QuotaController(_api);
        await controller.LoadAsync();
        var loaded = controller.SelectedMegabytes;

        // act
        controller.Select(10);
        var low = controller.SelectedMegabytes;
        controller.Select(1_000_000);

        // assert
        Assert.Equal(2048, loaded);
        Assert.Equal(QuotaController.MinimumMegabytes, low);
        Assert.Equal(3072, controller.SelectedMegabytes);
    }

    [Fact(DisplayName = "Saving sends the chosen space and says it was saved")]
    public async Task Save_SendsSelected()
    {
        // arrange
        var controller = new QuotaController(_api);
        await controller.LoadAsync();
        controller.Select(1000);

        // act
        await controller.SaveAsync();

        // assert
        Assert.True(controller.Saved);
        Assert.Equal(1000, _api.Quota.QuotaMb);
    }

    [Fact(DisplayName = "A failed save says so and keeps the choice")]
    public async Task Save_Offline_ReportsFailure()
    {
        // arrange
        var controller = new QuotaController(_api);
        await controller.LoadAsync();
        controller.Select(1000);
        _api.Offline = true;

        // act
        await controller.SaveAsync();

        // assert
        Assert.False(controller.Saved);
        Assert.Equal(1000, controller.SelectedMegabytes);
    }

    [Theory(DisplayName = "About 300 MB fit one hour of one camera, and the hours split among the cameras that record")]
    [InlineData(0, 2)]
    [InlineData(1, 2)]
    [InlineData(2, 1)]
    public void HoursFor_600Megabytes_SplitsAmongCameras(int cameras, double hours) =>
        Assert.Equal(hours, QuotaController.HoursFor(600, cameras));
}
