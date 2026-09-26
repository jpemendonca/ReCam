using Microsoft.AspNetCore.Components;
using Recam.Web.Live;

namespace Recam.Web.Tests.Support;

/// <summary>Video that starts after a given number of refusals, as while the camera opens.</summary>
public sealed class FakeLiveVideo : ILiveVideo
{
    public int RefusalsBeforePlaying { get; set; }

    public int Starts { get; private set; }

    public int Stops { get; private set; }

    public bool Disposed { get; private set; }

    public event Action? Ended;

    public bool? Muted { get; private set; }

    /// <summary>Like a browser before any click on the page: the sound stays off.</summary>
    public bool BlocksSound { get; set; }

    public Task<bool> SetMutedAsync(ElementReference video, bool muted)
    {
        Muted = muted || BlocksSound;
        return Task.FromResult(Muted.Value);
    }

    public Task<bool> StartAsync(ElementReference video, Guid cameraId)
    {
        Starts++;
        return Task.FromResult(Starts > RefusalsBeforePlaying);
    }

    public Task StopAsync()
    {
        Stops++;
        return Task.CompletedTask;
    }

    public void End() => Ended?.Invoke();

    public ValueTask DisposeAsync()
    {
        Disposed = true;
        return ValueTask.CompletedTask;
    }
}
