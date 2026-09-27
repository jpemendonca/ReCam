using Recam.Web.Api;

namespace Recam.Web.Tests.Support;

public static class Cameras
{
    public static CameraInfo Make(
        string name, bool online = true, bool publishing = false, bool recording = false, bool canRecord = true, bool torchOn = false,
        CameraRecordingState? recordingState = null) =>
        new(Guid.NewGuid(), name, online, publishing, 80, false, 31.6, DateTimeOffset.UnixEpoch, recording, canRecord, torchOn,
            recordingState ?? (!canRecord ? CameraRecordingState.NeedsH264 : recording ? CameraRecordingState.Recording : CameraRecordingState.Off));
}
