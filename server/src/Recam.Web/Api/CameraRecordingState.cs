using System.Text.Json.Serialization;

namespace Recam.Web.Api;

/// <summary>What is really happening with a camera's recording, as the server sees it on disk.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<CameraRecordingState>))]
public enum CameraRecordingState
{
    Off,
    NeedsH264,
    Offline,
    NoSpace,
    Starting,
    Recording,
    Stalled,
}
