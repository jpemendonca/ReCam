namespace Recam.Web.Live;

/// <summary>What happened when a live view tried to start.</summary>
public enum LiveStart
{
    /// <summary>Media flows.</summary>
    Playing,

    /// <summary>The server refused, usually because the camera is still opening: try again.</summary>
    NotYet,

    /// <summary>The server answered, but no media connection opened (network, codec, browser).</summary>
    MediaFailed,
}
