using System.Net.Http.Json;
using Recam.Server.Domain;
using Recam.Server.Features.Recordings;
using Recam.Server.Tests.Support;

namespace Recam.Server.Tests.Features.Recordings;

public sealed class RecordingCleanupWorkerTests
{
    private const long Megabyte = RecordingQuota.BytesPerMegabyte;
    private static readonly TimeSpan Wait = TimeSpan.FromSeconds(10);

    [Fact(DisplayName = "Every minute the oldest segments go until the rest fits, and removed cameras are cleared")]
    public async Task Cleanup_AfterAMinute_DeletesOldestAndRemovedCameras()
    {
        // arrange
        using var factory = new RecamApiFactory();
        var owner = await factory.PairDeviceAsync(DeviceRole.Owner);
        var camera = await factory.PairDeviceAsync(DeviceRole.Camera);
        using var client = factory.CreateDeviceClient(owner.Credential);
        using var quota = await client.PutAsJsonAsync(
            new Uri("/api/recordings/quota", UriKind.Relative), new { quotaMb = 100 }, ApiJson.Options, TestContext.Current.CancellationToken);
        var now = factory.Time.GetUtcNow();
        var oldest = RecordingFiles.Write(factory.RecordingsDirectory, camera.DeviceId, now.AddMinutes(-3), 60 * Megabyte);
        var middle = RecordingFiles.Write(factory.RecordingsDirectory, camera.DeviceId, now.AddMinutes(-2), 60 * Megabyte);
        var newest = RecordingFiles.Write(factory.RecordingsDirectory, camera.DeviceId, now.AddMinutes(-1), 60 * Megabyte);
        var removedCamera = Guid.NewGuid();
        var leftover = RecordingFiles.Write(factory.RecordingsDirectory, removedCamera, now.AddMinutes(-5), 1000);

        // act
        factory.Time.Advance(RecordingCleanupWorker.Interval);

        // assert
        await WaitUntilAsync(() => !File.Exists(middle) && !Directory.Exists(Path.GetDirectoryName(leftover)));
        Assert.False(File.Exists(oldest));
        Assert.True(File.Exists(newest));
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        using var timeout = new CancellationTokenSource(Wait);
        while (!condition())
        {
            await Task.Delay(50, timeout.Token);
        }
    }
}
