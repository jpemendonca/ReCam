using System.Security.Cryptography;
using DotNet.Testcontainers.Builders;
using Recam.Server.Infrastructure.Recordings;
using Recam.Server.Tests.Features.Media;
using Recam.Server.Tests.Support;

namespace Recam.Server.Tests.Features.Recordings;

/// <summary>A real recording, made by FFmpeg in MediaMTX's format, through the cipher and back.</summary>
public sealed class RecordingCipherVideoTests
{
    [Fact(DisplayName = "A ciphered recording is no video to FFmpeg, and deciphered it plays again")]
    public async Task Cipher_RealRecording_OnlyPlaysDeciphered()
    {
        // arrange
        using var folder = new TemporaryDirectory();
        const string name = "2026-09-27_10-00-00-000000.mp4";
        await RunAsync(folder.Path,
            "ffmpeg -loglevel error -f lavfi -i testsrc=size=640x360:rate=15 -f lavfi -i sine=frequency=440:sample_rate=48000 -t 4 " +
            "-c:v libx264 -g 15 -pix_fmt yuv420p -c:a libopus -movflags +frag_keyframe+empty_moov+default_base_moof " +
            $"/work/{name}");
        var cipher = new RecordingCipher(RandomNumberGenerator.GetBytes(32));
        var path = Path.Combine(folder.Path, name);

        // act
        cipher.CipherFile(path);
        await using (var plain = cipher.OpenRead(path))
        await using (var copy = File.Create(Path.Combine(folder.Path, "deciphered.mp4")))
        {
            await plain.CopyToAsync(copy, TestContext.Current.CancellationToken);
        }

        // assert
        var (cipheredExit, _) = await RunAsync(folder.Path, $"ffprobe -loglevel error -show_streams /work/{name}", mustSucceed: false);
        var (_, streams) = await RunAsync(folder.Path, "ffprobe -loglevel error -show_entries stream=codec_name -of csv=p=0 /work/deciphered.mp4");
        Assert.NotEqual(0, cipheredExit);
        Assert.Equal(["h264", "opus"], streams.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }

    private static async Task<(long ExitCode, string Output)> RunAsync(string folder, string command, bool mustSucceed = true)
    {
        var container = new ContainerBuilder(MediaMtxRecordingTests.PublisherImage)
            .WithBindMount(folder, "/work")
            .WithEntrypoint("sh", "-c")
            .WithCommand(command)
            .Build();
        await using (container)
        {
            await container.StartAsync(TestContext.Current.CancellationToken);
            var exitCode = await container.GetExitCodeAsync(TestContext.Current.CancellationToken);
            var (stdout, stderr) = await container.GetLogsAsync(timestampsEnabled: false, ct: TestContext.Current.CancellationToken);
            if (mustSucceed)
            {
                Assert.True(exitCode == 0, stderr);
            }

            return (exitCode, stdout);
        }
    }
}
