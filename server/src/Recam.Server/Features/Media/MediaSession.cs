namespace Recam.Server.Features.Media;

/// <summary>
/// A WHIP or WHEP session as the client sees it: MediaMTX's session id, prefixed with the path
/// kind it lives on. The prefix keeps later PATCH and DELETE on the right path after the camera
/// switched recording on or off.
/// </summary>
public sealed record MediaSession(bool Recorded, string Id)
{
    public const string LivePrefix = "cam";
    public const string RecordedPrefix = "rec";

    /// <summary>The MediaMTX path: <c>cam-{id}</c> relays, <c>rec-{id}</c> relays and records.</summary>
    public static string PathFor(Guid cameraId, bool recorded) =>
        $"{(recorded ? RecordedPrefix : LivePrefix)}-{cameraId:N}";

    /// <summary>Returns null for anything this proxy did not hand out.</summary>
    public static MediaSession? Parse(string value)
    {
        var separator = value.IndexOf('-', StringComparison.Ordinal);
        if (separator <= 0 || separator == value.Length - 1)
        {
            return null;
        }

        var id = value[(separator + 1)..];
        return value[..separator] switch
        {
            LivePrefix => new MediaSession(false, id),
            RecordedPrefix => new MediaSession(true, id),
            _ => null,
        };
    }

    public override string ToString() => $"{(Recorded ? RecordedPrefix : LivePrefix)}-{Id}";
}
