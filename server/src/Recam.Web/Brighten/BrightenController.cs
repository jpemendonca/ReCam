using Microsoft.AspNetCore.Components;

namespace Recam.Web.Brighten;

/// <summary>
/// "Brighten" for one camera in this browser: opens the sliders, remembers the setting per
/// camera and goes back to normal. The page calls <see cref="ApplyAsync"/> after it renders.
/// </summary>
public sealed class BrightenController(IBrightenSurface surface)
{
    private Guid _cameraId;

    public ImageAdjustment Adjustment { get; private set; } = ImageAdjustment.Normal;

    /// <summary>Whether the sliders show.</summary>
    public bool Open { get; private set; }

    public event Action? Changed;

    public async Task LoadAsync(Guid cameraId)
    {
        _cameraId = cameraId;
        Adjustment = await surface.LoadAsync(cameraId);
        Changed?.Invoke();
    }

    public void Toggle()
    {
        Open = !Open;
        Changed?.Invoke();
    }

    public Task SetBrightnessAsync(double value) => ChangeAsync(Adjustment.With(brightness: value));

    public Task SetContrastAsync(double value) => ChangeAsync(Adjustment.With(contrast: value));

    public Task ResetAsync() => ChangeAsync(ImageAdjustment.Normal);

    public Task ApplyAsync(ElementReference video) => surface.ApplyAsync(video, Adjustment);

    private async Task ChangeAsync(ImageAdjustment adjustment)
    {
        Adjustment = adjustment;
        Changed?.Invoke();
        await surface.SaveAsync(_cameraId, adjustment);
    }
}
