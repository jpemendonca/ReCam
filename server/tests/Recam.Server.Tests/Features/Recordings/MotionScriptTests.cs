using DotNet.Testcontainers.Builders;
using Recam.Server.Domain;
using Recam.Server.Infrastructure.Hosting;
using Recam.Server.Infrastructure.Recordings;
using Recam.Server.Tests.Features.Media;
using Recam.Server.Tests.Support;

namespace Recam.Server.Tests.Features.Recordings;

/// <summary>
/// Runs deploy/motion.sh in the image the composes use, on two segments made by FFmpeg: a still
/// scene with sensor noise, and the same scene with a person-sized shape walking across.
/// </summary>
public sealed class MotionScriptTests
{
    private static readonly DateTimeOffset Still = new(2026, 9, 26, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Walking = Still.AddSeconds(20);

    [Fact(DisplayName = "The motion service scores closed segments, and only the walk becomes an event")]
    public async Task MotionScript_StillAndWalking_OnlyWalkMoves()
    {
        // arrange
        using var recordings = new TemporaryDirectory();
        var cameraId = Guid.NewGuid();
        var folder = $"/recordings/rec-{cameraId:N}";
        const string scene = "color=c=0x505050:s=640x360:r=15,noise=alls=12:allf=t";
        var encode = "-t 20 -c:v libx264 -g 15 -pix_fmt yuv420p -movflags +frag_keyframe+empty_moov+default_base_moof";
        var script = string.Join(" && ", [
            "set -e",
            // the container runs as root; open what it writes so the runner user can delete it
            "trap 'chmod -R a+rwX /recordings' EXIT",
            $"mkdir -p {folder}",
            $"ffmpeg -loglevel error -f lavfi -i \"{scene}\" {encode} {folder}/{Name(Still)}",
            $"ffmpeg -loglevel error -f lavfi -i \"{scene}[bg];color=c=0xd0d0d0:s=50x140:r=15[box];" +
                $"[bg][box]overlay=x='if(between(t,5,12),(t-5)*80,-100)':y=120:shortest=1\" {encode} {folder}/{Name(Walking)}",
            "RECAM_MOTION_CLOSED_AFTER=0 bash /motion.sh --once",
        ]);
        var container = new ContainerBuilder(MediaMtxRecordingTests.PublisherImage)
            .WithResourceMapping(new FileInfo(MediaMtxFixture.RepositoryFile("deploy/motion.sh")), "/")
            .WithBindMount(recordings.Path, "/recordings")
            .WithEntrypoint("bash", "-c")
            .WithCommand(script)
            .Build();

        // act
        await using (container)
        {
            await container.StartAsync(TestContext.Current.CancellationToken);
            var exitCode = await container.GetExitCodeAsync(TestContext.Current.CancellationToken);
            var (_, stderr) = await container.GetLogsAsync(timestampsEnabled: false, ct: TestContext.Current.CancellationToken);
            Assert.True(exitCode == 0, stderr);
        }

        // assert
        var store = new RecordingStore(new ServerSettings(recordings.Path, [], null) { RecordingsDirectory = recordings.Path });
        var segments = store.ListSegments().OrderBy(segment => segment.StartsAt).ToList();
        Assert.Equal(2, segments.Count);
        var still = store.ReadMotion(segments[0]);
        var walking = store.ReadMotion(segments[1]);
        Assert.InRange(still.Count, 35, 40);
        Assert.Empty(MotionEvents.Find(still, MotionSensitivity.High, []));
        var walk = Assert.Single(MotionEvents.Find(walking, MotionSensitivity.Medium, []));
        Assert.InRange((walk.Start - Walking).TotalSeconds, 4.5, 6);
        Assert.InRange((walk.End - Walking).TotalSeconds, 11, 13);
    }

    private static string Name(DateTimeOffset startsAt) => $"{startsAt.UtcDateTime:yyyy-MM-dd_HH-mm-ss-ffffff}.mp4";
}
