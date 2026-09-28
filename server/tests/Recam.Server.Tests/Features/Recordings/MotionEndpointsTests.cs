using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Recam.Server.Domain;
using Recam.Server.Features.Recordings;
using Recam.Server.Infrastructure.Presence;
using Recam.Server.Tests.Support;

namespace Recam.Server.Tests.Features.Recordings;

public sealed class MotionEndpointsTests
{
    private static readonly DateTimeOffset Noon = new(2026, 9, 26, 12, 0, 0, TimeSpan.Zero);

    [Fact(DisplayName = "The day's motion comes from the scores, with the camera's sensitivity, which starts at medium")]
    public async Task GetMotion_WithScores_ReturnsEvents()
    {
        // arrange
        using var factory = new RecamApiFactory();
        var owner = await factory.PairDeviceAsync(DeviceRole.Owner);
        var camera = await factory.PairDeviceAsync(DeviceRole.Camera);
        WriteScores(factory, camera.DeviceId);
        using var client = factory.CreateDeviceClient(owner.Credential);

        // act
        var motion = await client.GetFromJsonAsync<MotionResponse>(MotionUri(camera.DeviceId), ApiJson.Options, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(MotionSensitivity.Medium, motion!.Sensitivity);
        Assert.Equal(2, motion.Events.Count);
        Assert.Equal(Noon.AddSeconds(1), motion.Events[0].Start);
        Assert.Equal(Noon.AddSeconds(60 + 30), motion.Events[1].Start);
    }

    [Fact(DisplayName = "Each event says whether the detect service saw a person, or null when it did not look")]
    public async Task GetMotion_WithPeopleFiles_MarksEvents()
    {
        // arrange
        using var factory = new RecamApiFactory();
        var owner = await factory.PairDeviceAsync(DeviceRole.Owner);
        var camera = await factory.PairDeviceAsync(DeviceRole.Camera);
        WriteScores(factory, camera.DeviceId);
        var first = Path.Combine(factory.RecordingsDirectory, $"rec-{camera.DeviceId:N}", $"{Noon.UtcDateTime:yyyy-MM-dd_HH-mm-ss-ffffff}.mp4");
        File.WriteAllText(first + ".people", "1\n2 0.88 0.1 0.2 0.3 0.4\n");
        using var client = factory.CreateDeviceClient(owner.Credential);

        // act
        var motion = await client.GetFromJsonAsync<MotionResponse>(MotionUri(camera.DeviceId), ApiJson.Options, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(2, motion!.Events.Count);
        Assert.True(motion.Events[0].Person);
        Assert.Null(motion.Events[1].Person);
    }

    [Fact(DisplayName = "A lower sensitivity also changes what was already recorded")]
    public async Task SetSensitivity_Low_DropsSmallMotion()
    {
        // arrange
        using var factory = new RecamApiFactory();
        var owner = await factory.PairDeviceAsync(DeviceRole.Owner);
        var camera = await factory.PairDeviceAsync(DeviceRole.Camera);
        WriteScores(factory, camera.DeviceId);
        using var client = factory.CreateDeviceClient(owner.Credential);

        // act
        using var changed = await client.PutAsJsonAsync(
            SensitivityUri(camera.DeviceId), new { sensitivity = "low" }, ApiJson.Options, TestContext.Current.CancellationToken);
        var motion = await client.GetFromJsonAsync<MotionResponse>(MotionUri(camera.DeviceId), ApiJson.Options, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.NoContent, changed.StatusCode);
        Assert.Equal(MotionSensitivity.Low, motion!.Sensitivity);
        Assert.Equal(Noon.AddSeconds(1), Assert.Single(motion.Events).Start);
    }

    [Fact(DisplayName = "Right after the flashlight switches, the jump in the picture is not motion")]
    public async Task GetMotion_AfterTorch_Ignored()
    {
        // arrange
        using var factory = new RecamApiFactory();
        var owner = await factory.PairDeviceAsync(DeviceRole.Owner);
        var camera = await factory.PairDeviceAsync(DeviceRole.Camera);
        WriteScores(factory, camera.DeviceId);
        factory.Services.GetRequiredService<DevicePresence>().RecordTorchChange(camera.DeviceId, Noon.AddSeconds(0.5));
        using var client = factory.CreateDeviceClient(owner.Credential);

        // act
        var motion = await client.GetFromJsonAsync<MotionResponse>(MotionUri(camera.DeviceId), ApiJson.Options, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(Noon.AddSeconds(60 + 30), Assert.Single(motion!.Events).Start);
    }

    [Fact(DisplayName = "Only a Monitor changes the sensitivity, and only to a known value")]
    public async Task SetSensitivity_ByCameraOrUnknown_Refused()
    {
        // arrange
        using var factory = new RecamApiFactory();
        var owner = await factory.PairDeviceAsync(DeviceRole.Owner);
        var camera = await factory.PairDeviceAsync(DeviceRole.Camera);
        using var cameraClient = factory.CreateDeviceClient(camera.Credential);
        using var ownerClient = factory.CreateDeviceClient(owner.Credential);

        // act
        using var byCamera = await cameraClient.PutAsJsonAsync(
            SensitivityUri(camera.DeviceId), new { sensitivity = "high" }, ApiJson.Options, TestContext.Current.CancellationToken);
        using var missing = await ownerClient.PutAsJsonAsync(
            SensitivityUri(camera.DeviceId), new { }, ApiJson.Options, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.Forbidden, byCamera.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);
    }

    // Two segments of noon: a clear walk at 1-4 s, and a small change at 30 s of the second.
    private static void WriteScores(RecamApiFactory factory, Guid cameraId)
    {
        var first = RecordingFiles.Write(factory.RecordingsDirectory, cameraId, Noon, 100);
        RecordingFiles.WriteMotion(first, (0.5, 0), (1, 0.05), (2, 0.06), (4, 0.04), (5, 0));
        var second = RecordingFiles.Write(factory.RecordingsDirectory, cameraId, Noon.AddSeconds(60), 100);
        RecordingFiles.WriteMotion(second, (29.5, 0), (30, 0.015), (30.5, 0));
    }

    private static Uri MotionUri(Guid cameraId) => new($"/api/cameras/{cameraId}/motion?day=2026-09-26", UriKind.Relative);

    private static Uri SensitivityUri(Guid cameraId) => new($"/api/cameras/{cameraId}/motion-sensitivity", UriKind.Relative);
}
