using Microsoft.Extensions.DependencyInjection;
using Recam.Web.Api;
using Recam.Web.Brighten;
using Recam.Web.Cameras;
using Recam.Web.Live;
using Recam.Web.Pages;
using Recam.Web.Realtime;
using Recam.Web.Tests.Support;

namespace Recam.Web.Tests.Brighten;

public sealed class BrightenTests : BunitContext
{
    private readonly FakeRecamApi _api = new() { Me = new MeInfo(Guid.NewGuid(), "Navegador", "owner") };
    private readonly FakeBrightenSurface _surface = new();
    private readonly CameraInfo _camera = Support.Cameras.Make("Porta", publishing: true);

    public BrightenTests()
    {
        _api.Cameras.Add(_camera);
        Services.AddLocalization();
        Services.AddSingleton<IRecamApi>(_api);
        Services.AddSingleton<IDeviceHub>(new FakeDeviceHub());
        Services.AddSingleton<ILiveVideo>(new FakeLiveVideo());
        Services.AddSingleton<IBrightenSurface>(_surface);
        Services.AddSingleton<CameraListController>();
        Services.AddSingleton<SoundChoice>();
        Services.AddTransient<LiveController>();
        Services.AddTransient<BrightenController>();
    }

    [Fact(DisplayName = "A change is remembered for that camera only, and kept inside the sliders")]
    public async Task Controller_Change_SavedPerCamera()
    {
        // arrange
        var controller = new BrightenController(_surface);
        await controller.LoadAsync(_camera.Id);

        // act
        await controller.SetBrightnessAsync(9);
        await controller.SetContrastAsync(0.2);
        var again = new BrightenController(_surface);
        await again.LoadAsync(_camera.Id);
        var other = new BrightenController(_surface);
        await other.LoadAsync(Guid.NewGuid());

        // assert
        Assert.Equal(new ImageAdjustment(ImageAdjustment.MaxBrightness, ImageAdjustment.MinContrast), again.Adjustment);
        Assert.True(other.Adjustment.IsNormal);
    }

    [Fact(DisplayName = "The setting travels as text and comes back the same; garbage is normal")]
    public void Adjustment_EncodeDecode()
    {
        // arrange
        var adjustment = new ImageAdjustment(1.5, 0.8);

        // act
        var decoded = ImageAdjustment.Decode(adjustment.Encode());

        // assert
        Assert.Equal(adjustment, decoded);
        Assert.True(ImageAdjustment.Decode("oops").IsNormal);
    }

    [Fact(DisplayName = "In the live view, Brighten opens the sliders and filters the video; Back to normal undoes it")]
    public void LiveView_Brighten_FiltersAndResets()
    {
        // arrange
        using var _ = Culture.Use("pt-BR");
        var page = Render<LiveView>(parameters => parameters.Add(view => view.CameraId, _camera.Id));
        page.WaitForAssertion(() => Assert.Empty(page.FindAll(".overlay")));

        // act
        page.Find("button.brighten").Click();
        page.Find("input.brightness").Input("150");
        var afterBrightening = _surface.Applied[^1];
        page.Find("button.reset").Click();

        // assert
        Assert.Equal(new ImageAdjustment(1.5, 1), afterBrightening);
        Assert.Contains("Brilho", page.Find(".panel").TextContent, StringComparison.Ordinal);
        page.WaitForAssertion(() => Assert.True(_surface.Applied[^1].IsNormal));
        Assert.Empty(_surface.Saved);
    }
}
