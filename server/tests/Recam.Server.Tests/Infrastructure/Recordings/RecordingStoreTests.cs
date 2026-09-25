using Recam.Server.Infrastructure.Hosting;
using Recam.Server.Infrastructure.Recordings;
using Recam.Server.Tests.Support;

namespace Recam.Server.Tests.Infrastructure.Recordings;

public sealed class RecordingStoreTests
{
    [Fact(DisplayName = "Segments are read from MediaMTX's folder and file names, in UTC")]
    public void ListSegments_WithMediaMtxNames_ReadsCameraStartAndSize()
    {
        // arrange
        using var directory = new TemporaryDirectory();
        var store = new RecordingStore(new ServerSettings(directory.Path, [], null) { RecordingsDirectory = directory.Path });
        var cameraId = Guid.NewGuid();
        var startsAt = new DateTimeOffset(2026, 9, 25, 14, 3, 7, TimeSpan.Zero).AddTicks(1234560);
        RecordingFiles.Write(directory.Path, cameraId, startsAt, 4096);

        // act
        var segment = Assert.Single(store.ListSegments());

        // assert
        Assert.Equal(cameraId, segment.CameraId);
        Assert.Equal("2026-09-25_14-03-07-123456.mp4", segment.FileName);
        Assert.Equal(startsAt, segment.StartsAt);
        Assert.Equal(4096, segment.Bytes);
    }

    [Fact(DisplayName = "Files and folders that are not recordings are ignored")]
    public void ListSegments_WithForeignFiles_IgnoresThem()
    {
        // arrange
        using var directory = new TemporaryDirectory();
        var store = new RecordingStore(new ServerSettings(directory.Path, [], null) { RecordingsDirectory = directory.Path });
        Directory.CreateDirectory(Path.Combine(directory.Path, "cam-0123456789abcdef0123456789abcdef"));
        File.WriteAllText(Path.Combine(directory.Path, "notes.txt"), "x");
        var cameraFolder = Path.Combine(directory.Path, "rec-0123456789abcdef0123456789abcdef");
        Directory.CreateDirectory(cameraFolder);
        File.WriteAllText(Path.Combine(cameraFolder, "../evil.mp4"), "x");
        File.WriteAllText(Path.Combine(cameraFolder, "readme.mp4"), "x");

        // act
        var segments = store.ListSegments();

        // assert
        Assert.Empty(segments);
    }
}
