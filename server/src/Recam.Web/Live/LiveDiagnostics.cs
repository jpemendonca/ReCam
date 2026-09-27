namespace Recam.Web.Live;

/// <summary>
/// The browser's view of one live connection, for the diagnostics panel: raw values the person
/// can screenshot and send. Null fields are what the browser does not report.
/// </summary>
public sealed record LiveDiagnostics(
    string? Connection,
    string? Ice,
    string? Path,
    long? BytesReceived,
    long? FramesReceived,
    long? FramesDecoded,
    string? Codec,
    int? Width,
    int? Height,
    string? Player,
    string? PlayError);
