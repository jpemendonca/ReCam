using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using Recam.Server.Domain;
using Recam.Server.Features.Recordings;
using Recam.Server.Infrastructure.Recordings;
using Recam.Server.Tests.Support;

namespace Recam.Server.Tests.Features.Recordings;

public sealed class RecordingCipherWorkerTests
{
    [Fact(DisplayName = "A file is ciphered only once motion scored it, or once it is old enough")]
    public async Task CipherClosed_WaitsForMotionOrAge()
    {
        // arrange
        using var factory = new RecamApiFactory();
        var camera = await factory.PairDeviceAsync(DeviceRole.Camera);
        var now = factory.Time.GetUtcNow();
        var scored = RecordingFiles.Write(factory.RecordingsDirectory, camera.DeviceId, now.AddMinutes(-3), 100);
        RecordingFiles.WriteMotion(scored, (0.5, 0.01));
        var waiting = RecordingFiles.Write(factory.RecordingsDirectory, camera.DeviceId, now.AddMinutes(-2), 100);
        var old = RecordingFiles.Write(factory.RecordingsDirectory, camera.DeviceId, now - RecordingCipherWorker.GiveUpOnMotion - TimeSpan.FromMinutes(1), 100);
        var worker = factory.Services.GetRequiredService<RecordingCipherWorker>();

        // act
        worker.CipherClosed();

        // assert
        Assert.True(RecordingCipher.IsCiphered(scored));
        Assert.False(RecordingCipher.IsCiphered(waiting));
        Assert.True(RecordingCipher.IsCiphered(old));
        Assert.True(File.Exists(Path.Combine(factory.DataDirectory, RecordingCipher.KeyFileName)));
    }

    [Fact(DisplayName = "While the detect service runs, a file waits for its people too, but not forever")]
    public async Task CipherClosed_DetectRunning_WaitsForPeopleOrAge()
    {
        // arrange
        using var factory = new RecamApiFactory();
        var camera = await factory.PairDeviceAsync(DeviceRole.Camera);
        var now = factory.Time.GetUtcNow();
        var looked = RecordingFiles.Write(factory.RecordingsDirectory, camera.DeviceId, now.AddMinutes(-3), 100);
        RecordingFiles.WriteMotion(looked, (0.5, 0.01));
        File.WriteAllText(looked + RecordingStore.PeopleSuffix, "0\n");
        var waiting = RecordingFiles.Write(factory.RecordingsDirectory, camera.DeviceId, now.AddMinutes(-2), 100);
        RecordingFiles.WriteMotion(waiting, (0.5, 0.01));
        var old = RecordingFiles.Write(factory.RecordingsDirectory, camera.DeviceId, now - RecordingCipherWorker.GiveUpOnMotion - TimeSpan.FromMinutes(1), 100);
        RecordingFiles.WriteMotion(old, (0.5, 0.01));
        RecordingFiles.WriteDetectHeartbeat(factory.RecordingsDirectory, now.AddMinutes(-1));
        var worker = factory.Services.GetRequiredService<RecordingCipherWorker>();

        // act
        worker.CipherClosed();

        // assert
        Assert.True(RecordingCipher.IsCiphered(looked));
        Assert.False(RecordingCipher.IsCiphered(waiting));
        Assert.True(RecordingCipher.IsCiphered(old));
    }

    [Fact(DisplayName = "A detect service that stopped no longer holds files back")]
    public async Task CipherClosed_DetectStale_DoesNotWaitForPeople()
    {
        // arrange
        using var factory = new RecamApiFactory();
        var camera = await factory.PairDeviceAsync(DeviceRole.Camera);
        var now = factory.Time.GetUtcNow();
        var scored = RecordingFiles.Write(factory.RecordingsDirectory, camera.DeviceId, now.AddMinutes(-2), 100);
        RecordingFiles.WriteMotion(scored, (0.5, 0.01));
        RecordingFiles.WriteDetectHeartbeat(factory.RecordingsDirectory, now - RecordingCipherWorker.DetectStale - TimeSpan.FromMinutes(1));
        var worker = factory.Services.GetRequiredService<RecordingCipherWorker>();

        // act
        worker.CipherClosed();

        // assert
        Assert.True(RecordingCipher.IsCiphered(scored));
    }

    [Fact(DisplayName = "A ciphered recording still plays through the server, in ranges")]
    public async Task ServeSegment_Ciphered_ServesPlainRanges()
    {
        // arrange
        using var factory = new RecamApiFactory();
        var owner = await factory.PairDeviceAsync(DeviceRole.Owner);
        var camera = await factory.PairDeviceAsync(DeviceRole.Camera);
        var startsAt = factory.Time.GetUtcNow().AddMinutes(-3);
        var path = RecordingFiles.Write(factory.RecordingsDirectory, camera.DeviceId, startsAt, 0);
        var content = Enumerable.Range(0, 1000).Select(value => (byte)value).ToArray();
        await File.WriteAllBytesAsync(path, content, TestContext.Current.CancellationToken);
        RecordingFiles.WriteMotion(path, (0.5, 0.01));
        factory.Services.GetRequiredService<RecordingCipherWorker>().CipherClosed();
        using var client = factory.CreateDeviceClient(owner.Credential);
        using var request = new HttpRequestMessage(
            HttpMethod.Get, new Uri($"/api/recordings/{camera.DeviceId}/{Path.GetFileName(path)}", UriKind.Relative));
        request.Headers.Range = new RangeHeaderValue(13, 540);

        // act
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.True(RecordingCipher.IsCiphered(path));
        Assert.Equal(HttpStatusCode.PartialContent, response.StatusCode);
        Assert.Equal(1000, response.Content.Headers.ContentRange?.Length);
        Assert.Equal(content[13..541], await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken));
    }
}
