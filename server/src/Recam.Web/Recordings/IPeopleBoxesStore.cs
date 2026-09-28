namespace Recam.Web.Recordings;

/// <summary>Keeps "Show people" in this browser. Faked in tests.</summary>
public interface IPeopleBoxesStore
{
    /// <summary>Whether the boxes around people show; on when nothing was saved.</summary>
    Task<bool> ReadAsync();

    Task SaveAsync(bool show);
}
