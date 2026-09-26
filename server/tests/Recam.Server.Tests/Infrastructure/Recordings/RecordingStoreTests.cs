using Recam.Server.Domain;
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

    [Fact(DisplayName = "Motion scores are read next to their segment, at the segment's time")]
    public void ReadMotion_WithScores_PlacesThemInTime()
    {
        // arrange
        using var directory = new TemporaryDirectory();
        var store = new RecordingStore(new ServerSettings(directory.Path, [], null) { RecordingsDirectory = directory.Path });
        var startsAt = new DateTimeOffset(2026, 9, 26, 12, 0, 0, TimeSpan.Zero);
        var path = RecordingFiles.Write(directory.Path, Guid.NewGuid(), startsAt, 10);
        RecordingFiles.WriteMotion(path, (0.5, 0.0), (1.0, 0.0512));
        File.AppendAllText(path + ".motion", "garbage line\n");

        // act
        var samples = store.ReadMotion(Assert.Single(store.ListSegments()));

        // assert
        Assert.Equal([new MotionSample(startsAt.AddSeconds(0.5), 0), new MotionSample(startsAt.AddSeconds(1), 0.0512)], samples);
    }

    [Fact(DisplayName = "Deleting a segment deletes its scores, and scores left without a segment are cleaned up")]
    public void Delete_AndOrphans_RemoveScores()
    {
        // arrange
        using var directory = new TemporaryDirectory();
        var store = new RecordingStore(new ServerSettings(directory.Path, [], null) { RecordingsDirectory = directory.Path });
        var cameraId = Guid.NewGuid();
        var kept = RecordingFiles.Write(directory.Path, cameraId, DateTimeOffset.UnixEpoch.AddDays(1), 10);
        var deleted = RecordingFiles.Write(directory.Path, cameraId, DateTimeOffset.UnixEpoch.AddDays(2), 10);
        RecordingFiles.WriteMotion(kept, (0.5, 0.1));
        RecordingFiles.WriteMotion(deleted, (0.5, 0.1));
        var orphan = Path.Combine(Path.GetDirectoryName(kept)!, "2020-01-01_00-00-00-000000.mp4.motion");
        File.WriteAllText(orphan, "0.5 0.1");

        // act
        store.Delete(store.ListSegments().Single(segment => segment.FileName == Path.GetFileName(deleted)));
        store.DeleteOrphanMotion();

        // assert
        Assert.False(File.Exists(deleted + ".motion"));
        Assert.False(File.Exists(orphan));
        Assert.True(File.Exists(kept + ".motion"));
    }
}
