namespace Recam.Web.Live;

/// <summary>
/// Whether the person wants to hear the cameras, kept while the tab is open so every live view
/// opens the way the last one was left. Like the torch before it, the button must show the truth.
/// </summary>
public sealed class SoundChoice
{
    public bool Wanted { get; set; }
}
