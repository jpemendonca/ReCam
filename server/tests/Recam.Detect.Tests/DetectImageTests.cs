using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Images;

namespace Recam.Detect.Tests;

/// <summary>
/// Builds the detect image from its Dockerfile and runs one pass over segments made by FFmpeg
/// from two photos: one with a person, one without.
/// </summary>
public sealed class DetectImageTests
{
    private const string FfmpegImage = "linuxserver/ffmpeg:9.0-cli-ls83";
    private const string Camera = "rec-0123456789abcdef0123456789abcdef";
    private const string WithPerson = "2026-09-28_12-00-00-000000.mp4";
    private const string WithoutPerson = "2026-09-28_12-01-00-000000.mp4";
    private const string Ciphered = "2026-09-28_12-02-00-000000.mp4";
    private const string Unscored = "2026-09-28_12-03-00-000000.mp4";

    [Fact(DisplayName = "The detect service finds the person, not the dog, and skips what it cannot read")]
    public async Task DetectOnce_PhotoSegments_WritesPeopleFiles()
    {
        // arrange
        var cancellation = TestContext.Current.CancellationToken;
        var recordings = Directory.CreateTempSubdirectory("recam-detect-");
        try
        {
            var folder = Directory.CreateDirectory(Path.Combine(recordings.FullName, Camera));
            if (!OperatingSystem.IsWindows())
            {
                // The service runs as user 1654 and writes next to the segments.
                File.SetUnixFileMode(recordings.FullName, (UnixFileMode)0x1FF);
                File.SetUnixFileMode(folder.FullName, (UnixFileMode)0x1FF);
            }
            await EncodeAsync(folder.FullName, cancellation);
            await File.WriteAllTextAsync(Path.Combine(folder.FullName, Ciphered), "RECAMENC and the rest", cancellation);
            await File.WriteAllTextAsync(Path.Combine(folder.FullName, Unscored), "no motion scores yet", cancellation);
            foreach (var name in (string[])[WithPerson, WithoutPerson, Ciphered])
            {
                await File.WriteAllTextAsync(Path.Combine(folder.FullName, name + ".motion"), "0.5 0.05\n1.0 0.05\n3.5 0.0\n", cancellation);
            }

            var image = new ImageFromDockerfileBuilder()
                .WithDockerfileDirectory(ServerDirectory())
                .WithDockerfile("src/Recam.Detect/Dockerfile")
                .WithName("recam-detect:test")
                .WithCleanUp(false)
                .Build();
            await image.CreateAsync(cancellation);
            var container = new ContainerBuilder(image)
                .WithBindMount(recordings.FullName, "/recordings")
                .WithCommand("--once")
                .Build();

            // act
            await using (container)
            {
                await container.StartAsync(cancellation);
                var exitCode = await container.GetExitCodeAsync(cancellation);
                var (stdout, stderr) = await container.GetLogsAsync(timestampsEnabled: false, ct: cancellation);
                Assert.True(exitCode == 0, stdout + stderr);
            }

            // assert
            var person = await File.ReadAllLinesAsync(Path.Combine(folder.FullName, WithPerson + ".people"), cancellation);
            Assert.Equal(2, person.Length);
            var values = person[0].Split(' ').Select(value => double.Parse(value, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
            Assert.Equal(0, values[0]);
            Assert.Equal(6, values.Length);
            Assert.InRange(values[1], 0.5, 1);
            // The man stands left of center in the photo.
            Assert.InRange(values[2], 0.2, 0.4);
            Assert.InRange(values[4], 0.08, 0.2);
            Assert.InRange(values[5], 0.5, 0.8);

            var dog = await File.ReadAllLinesAsync(Path.Combine(folder.FullName, WithoutPerson + ".people"), cancellation);
            Assert.Equal(["0", "1"], dog.Select(line => line.Split(' ')[0]));
            Assert.All(dog, line => Assert.All(Confidences(line), confidence => Assert.True(confidence < 0.5, line)));

            Assert.False(File.Exists(Path.Combine(folder.FullName, Ciphered + ".people")));
            Assert.False(File.Exists(Path.Combine(folder.FullName, Unscored + ".people")));
            Assert.True(File.Exists(Path.Combine(recordings.FullName, ".detect")));
        }
        finally
        {
            recordings.Delete(recursive: true);
        }
    }

    /// <summary>Every fifth value after the seconds is a person's confidence.</summary>
    private static IEnumerable<double> Confidences(string line) =>
        line.Split(' ').Skip(1).Where((_, index) => index % 5 == 0)
            .Select(value => double.Parse(value, System.Globalization.CultureInfo.InvariantCulture));

    private static async Task EncodeAsync(string folder, CancellationToken cancellation)
    {
        const string encode = "-t 4 -r 15 -c:v libx264 -pix_fmt yuv420p -vf 'scale=trunc(iw/2)*2:trunc(ih/2)*2'";
        var container = new ContainerBuilder(FfmpegImage)
            .WithResourceMapping(new FileInfo(Path.Combine(AppContext.BaseDirectory, "Fixtures", "person.jpg")), "/photos/")
            .WithResourceMapping(new FileInfo(Path.Combine(AppContext.BaseDirectory, "Fixtures", "dog.jpg")), "/photos/")
            .WithBindMount(folder, "/out")
            .WithEntrypoint("bash", "-c")
            .WithCommand(
                $"set -e; ffmpeg -loglevel error -loop 1 -i /photos/person.jpg {encode} /out/{WithPerson}; " +
                $"ffmpeg -loglevel error -loop 1 -i /photos/dog.jpg {encode} /out/{WithoutPerson}; chmod 666 /out/*.mp4")
            .Build();
        await using (container)
        {
            await container.StartAsync(cancellation);
            var exitCode = await container.GetExitCodeAsync(cancellation);
            var (_, stderr) = await container.GetLogsAsync(timestampsEnabled: false, ct: cancellation);
            Assert.True(exitCode == 0, stderr);
        }
    }

    private static string ServerDirectory()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Recam.slnx")))
            {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException("Could not find server/Recam.slnx above the test output folder.");
    }
}
