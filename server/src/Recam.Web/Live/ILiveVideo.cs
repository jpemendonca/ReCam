using Microsoft.AspNetCore.Components;

namespace Recam.Web.Live;

/// <summary>Plays one camera's live video in a &lt;video&gt; element. Faked in tests.</summary>
public interface ILiveVideo : IAsyncDisposable
{
    /// <summary>Called when a playing stream drops (camera went away, network lost).</summary>
    event Action? Ended;

    /// <summary>False while the camera is not publishing yet, or when the server refuses.</summary>
    Task<bool> StartAsync(ElementReference video, Guid cameraId);

    Task StopAsync();

    /// <summary>Turns the camera's sound on or off in the &lt;video&gt;.</summary>
    Task SetMutedAsync(ElementReference video, bool muted);
}
