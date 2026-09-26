using Recam.Web.Api;

namespace Recam.Web.Tests.Support;

public static class Cameras
{
    public static CameraInfo Make(string name, bool online = true, bool publishing = false, bool recording = false, bool canRecord = true) =>
        new(Guid.NewGuid(), name, online, publishing, 80, false, 31.6, DateTimeOffset.UnixEpoch, recording, canRecord);
}
