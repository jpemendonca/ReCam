using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using DotNet.Testcontainers.Volumes;
using Recam.Server.Tests.Support;

namespace Recam.Server.Tests.Features.Media;

/// <summary>
/// Publishes a real H.264 stream by WHIP to MediaMTX, with deploy/mediamtx.yml and the same
/// non-root user as production, and looks at what lands in the recordings volume.
/// </summary>
public sealed class MediaMtxRecordingTests : IAsyncLifetime
{
    // FFmpeg's WHIP muxer publishes H.264 and Opus; it stands in for the camera phone.
    public const string PublisherImage = "linuxserver/ffmpeg:9.0-cli-ls83";
    private const string ServerUser = "1654:1654";
    private const string MediaMtxAlias = "mediamtx";

    private readonly INetwork _network = new NetworkBuilder().Build();
    private readonly IVolume _recordings = new VolumeBuilder().Build();
    private IContainer? _mediaMtx;

    public async ValueTask InitializeAsync()
    {
        await _network.CreateAsync();
        await _recordings.CreateAsync();

        // In production the server image creates the volume already owned by this user.
        await RunToEndAsync(Tool("chown 1654:1654 /recordings"));

        _mediaMtx = new ContainerBuilder(MediaMtxFixture.Image)
            .WithResourceMapping(new FileInfo(MediaMtxFixture.RepositoryFile("deploy/mediamtx.yml")), "/")
            .WithCreateParameterModifier(parameters => parameters.User = ServerUser)
            // FFmpeg only tries the first ICE candidate; keep loopback out of the answer.
            .WithEnvironment("MTX_WEBRTCIPSFROMINTERFACESLIST", "eth0")
            .WithNetwork(_network)
            .WithNetworkAliases(MediaMtxAlias)
            .WithVolumeMount(_recordings, "/recordings")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("WebRTC\\] started"))
            .Build();
        await _mediaMtx.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_mediaMtx is not null)
        {
            await _mediaMtx.DisposeAsync();
        }

        await _recordings.DisposeAsync();
        await _network.DisposeAsync();
    }

    [Fact(DisplayName = "A stream published to a rec- path is recorded as fMP4, and a cam- path is not recorded")]
    public async Task Publish_ToRecPath_WritesFmp4Segment()
    {
        // arrange
        var cameraId = Guid.NewGuid().ToString("N");

        // act
        await RunToEndAsync(Publisher($"rec-{cameraId}"));
        await RunToEndAsync(Publisher($"cam-{cameraId}"));
        var listing = await RunToEndAsync(Tool(
            "for f in $(find /recordings -type f); do echo \"$f $(head -c 8 \"$f\" | tail -c 4)\"; done"));

        // assert
        var files = listing.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var recorded = Assert.Single(files, file => file.StartsWith($"/recordings/rec-{cameraId}/", StringComparison.Ordinal));
        Assert.EndsWith(".mp4 ftyp", recorded, StringComparison.Ordinal);
        Assert.DoesNotContain(files, file => file.Contains($"cam-{cameraId}", StringComparison.Ordinal));
    }

    private IContainer Publisher(string path, bool withAudio = false) =>
        new ContainerBuilder(PublisherImage)
            .WithEntrypoint("ffmpeg")
            .WithCommand([
                "-hide_banner", "-loglevel", "warning", "-re",
                "-f", "lavfi", "-i", "testsrc=size=640x360:rate=15", "-t", "6",
                .. withAudio ? ["-f", "lavfi", "-i", "sine=frequency=440:sample_rate=48000", "-t", "6"] : Array.Empty<string>(),
                "-c:v", "libx264", "-profile:v", "baseline", "-bf", "0", "-g", "15", "-pix_fmt", "yuv420p",
                .. withAudio ? ["-c:a", "libopus", "-ar", "48000", "-ac", "2"] : Array.Empty<string>(),
                "-f", "whip", $"http://{MediaMtxAlias}:8889/{path}/whip"])
            .WithNetwork(_network)
            .Build();

    [Fact(DisplayName = "The camera's Opus audio is recorded in the same fMP4 file as the video")]
    public async Task Publish_WithAudio_RecordsOpusTrack()
    {
        // arrange
        var cameraId = Guid.NewGuid().ToString("N");

        // act
        await RunToEndAsync(Publisher($"rec-{cameraId}", withAudio: true));
        var tracks = await RunToEndAsync(Tool(
            $"for f in $(find /recordings/rec-{cameraId} -type f); do grep -c -a -e avc1 \"$f\"; grep -c -a -e Opus \"$f\"; done"));

        // assert
        var counts = tracks.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        Assert.Equal(2, counts.Length);
        Assert.All(counts, count => Assert.NotEqual("0", count));
    }

    private IContainer Tool(string script) =>
        new ContainerBuilder(PublisherImage)
            .WithEntrypoint("sh", "-c")
            .WithCommand(script)
            .WithVolumeMount(_recordings, "/recordings")
            .Build();

    /// <summary>Starts a short-lived container, waits for it to finish and returns its output.</summary>
    private static async Task<string> RunToEndAsync(IContainer container)
    {
        await using (container)
        {
            await container.StartAsync(TestContext.Current.CancellationToken);
            var exitCode = await container.GetExitCodeAsync(TestContext.Current.CancellationToken);
            var (stdout, stderr) = await container.GetLogsAsync(
                timestampsEnabled: false, ct: TestContext.Current.CancellationToken);
            Assert.True(exitCode == 0, $"{container.Image.FullName} exited with {exitCode}: {stderr}");
            return stdout;
        }
    }
}
