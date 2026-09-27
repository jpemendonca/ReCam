using Microsoft.AspNetCore.Components;

namespace Recam.Web.Live;

/// <summary>Plays one camera's live video in a &lt;video&gt; element. Faked in tests.</summary>
public interface ILiveVideo : IAsyncDisposable
{
    /// <summary>Called when a playing stream drops (camera went away, network lost).</summary>
    event Action? Ended;

    Task<LiveStart> StartAsync(ElementReference video, Guid cameraId);

    /// <summary>The playing connection as the browser sees it; null when nothing plays.</summary>
    Task<LiveDiagnostics?> DiagnosticsAsync();

    Task StopAsync();

    /// <summary>
    /// Turns the camera's sound on or off in the &lt;video&gt;. Returns whether it ended up muted:
    /// before any click on the page, the browser keeps it silent.
    /// </summary>
    Task<bool> SetMutedAsync(ElementReference video, bool muted);
}
