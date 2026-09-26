namespace Recam.Web.Cameras;

public enum CameraListState
{
    Loading,
    Loaded,

    /// <summary>The first load failed; a list already shown stays on later failures.</summary>
    Failed,

    /// <summary>The server no longer knows this browser (removed or signed out elsewhere).</summary>
    SignedOut,
}
