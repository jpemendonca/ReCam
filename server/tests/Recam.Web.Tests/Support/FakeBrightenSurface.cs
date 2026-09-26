using Microsoft.AspNetCore.Components;
using Recam.Web.Brighten;

namespace Recam.Web.Tests.Support;

/// <summary>localStorage and the video filter, in memory.</summary>
public sealed class FakeBrightenSurface : IBrightenSurface
{
    public Dictionary<Guid, ImageAdjustment> Saved { get; } = [];

    /// <summary>Every filter put on a video, in order.</summary>
    public List<ImageAdjustment> Applied { get; } = [];

    public Task<ImageAdjustment> LoadAsync(Guid cameraId) => Task.FromResult(Saved.GetValueOrDefault(cameraId) ?? ImageAdjustment.Normal);

    public Task SaveAsync(Guid cameraId, ImageAdjustment adjustment)
    {
        if (adjustment.IsNormal)
        {
            Saved.Remove(cameraId);
        }
        else
        {
            Saved[cameraId] = adjustment;
        }

        return Task.CompletedTask;
    }

    public Task ApplyAsync(ElementReference video, ImageAdjustment adjustment)
    {
        Applied.Add(adjustment);
        return Task.CompletedTask;
    }
}
