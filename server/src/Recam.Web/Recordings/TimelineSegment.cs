namespace Recam.Web.Recordings;

/// <summary>
/// A recorded file placed on this computer's clock: <see cref="Start"/> and <see cref="End"/> are
/// wall-clock times, so a day runs from its midnight to the next one.
/// </summary>
/// <param name="Piece">Segments of the same piece are back to back.</param>
public sealed record TimelineSegment(DateTime Start, DateTime End, string Url, int Piece);
