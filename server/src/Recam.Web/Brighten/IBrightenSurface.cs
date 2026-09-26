using Microsoft.AspNetCore.Components;

namespace Recam.Web.Brighten;

/// <summary>Where "Brighten" lives in the browser: the video's filter and the saved setting. Faked in tests.</summary>
public interface IBrightenSurface
{
    Task<ImageAdjustment> LoadAsync(Guid cameraId);

    Task SaveAsync(Guid cameraId, ImageAdjustment adjustment);

    Task ApplyAsync(ElementReference video, ImageAdjustment adjustment);
}
