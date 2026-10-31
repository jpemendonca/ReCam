using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Recam.Server.Domain;
using Recam.Server.Features.Recordings;
using Recam.Server.Infrastructure.Recordings;
using Recam.Server.Tests.Support;

namespace Recam.Server.Tests.Features.Recordings;

public sealed class RecordingEndpointsTests
{
    [Fact(DisplayName = "A Monitor gets the camera's newest picture, deciphered, with when it was filmed")]
    public async Task GetThumbnail_AsMonitor_ReturnsNewestPicture()
    {
        // arrange
        using var factory = new RecamApiFactory();
        var owner = await factory.PairDeviceAsync(DeviceRole.Owner);
        var camera = await factory.PairDeviceAsync(DeviceRole.Camera);
        var now = factory.Time.GetUtcNow();
        var older = RecordingFiles.Write(factory.RecordingsDirectory, camera.DeviceId, now.AddMinutes(-3), 100);
        var newer = RecordingFiles.Write(factory.RecordingsDirectory, camera.DeviceId, now.AddMinutes(-2), 100);
        File.WriteAllBytes(older + RecordingStore.ThumbnailSuffix, [0xFF, 0xD8, 1, 0xFF, 0xD9]);
        File.WriteAllBytes(newer + RecordingStore.ThumbnailSuffix, [0xFF, 0xD8, 2, 0xFF, 0xD9]);
        var filmed = new DateTime(2026, 10, 31, 12, 0, 0, DateTimeKind.Utc);
        File.SetLastWriteTimeUtc(newer, filmed);
        factory.Services.GetRequiredService<RecordingCipher>().CipherFile(newer + RecordingStore.ThumbnailSuffix);
        using var client = factory.CreateDeviceClient(owner.Credential);

        // act
        using var response = await client.GetAsync($"/api/cameras/{camera.DeviceId}/thumbnail", TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("image/jpeg", response.Content.Headers.ContentType!.MediaType);
        Assert.Equal(new DateTimeOffset(filmed), response.Content.Headers.LastModified);
        Assert.Equal([0xFF, 0xD8, 2, 0xFF, 0xD9], await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken));
        Assert.True(RecordingCipher.IsCiphered(newer + RecordingStore.ThumbnailSuffix));
    }

    [Fact(DisplayName = "No picture yet, an unknown camera and a camera asking are all refused")]
    public async Task GetThumbnail_NoneUnknownOrCamera_Refused()
    {
        // arrange
        using var factory = new RecamApiFactory();
        var owner = await factory.PairDeviceAsync(DeviceRole.Owner);
        var camera = await factory.PairDeviceAsync(DeviceRole.Camera);
        RecordingFiles.Write(factory.RecordingsDirectory, camera.DeviceId, factory.Time.GetUtcNow().AddMinutes(-2), 100);
        using var client = factory.CreateDeviceClient(owner.Credential);
        using var cameraClient = factory.CreateDeviceClient(camera.Credential);

        // act
        using var none = await client.GetAsync($"/api/cameras/{camera.DeviceId}/thumbnail", TestContext.Current.CancellationToken);
        using var unknown = await client.GetAsync($"/api/cameras/{Guid.NewGuid()}/thumbnail", TestContext.Current.CancellationToken);
        using var byCamera = await cameraClient.GetAsync($"/api/cameras/{camera.DeviceId}/thumbnail", TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.NotFound, none.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, byCamera.StatusCode);
    }

    private static readonly Uri QuotaUri = new("/api/recordings/quota", UriKind.Relative);

    [Fact(DisplayName = "A Monitor reads the quota, the space in use and the free disk")]
    public async Task GetQuota_AsMonitor_ReturnsDefaultAndUsage()
    {
        // arrange
        using var factory = new RecamApiFactory();
        var owner = await factory.PairDeviceAsync(DeviceRole.Owner);
        var camera = await factory.PairDeviceAsync(DeviceRole.Camera);
        RecordingFiles.Write(factory.RecordingsDirectory, camera.DeviceId, factory.Time.GetUtcNow(), 5000);
        using var client = factory.CreateDeviceClient(owner.Credential);

        // act
        var quota = await client.GetFromJsonAsync<QuotaResponse>(QuotaUri, ApiJson.Options, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(RecordingQuota.DefaultMegabytes, quota!.QuotaMb);
        Assert.Equal(5000, quota.UsedBytes);
        Assert.True(quota.FreeBytes > 0);
    }

    [Fact(DisplayName = "A Monitor changes the quota")]
    public async Task PutQuota_WithinDisk_ChangesIt()
    {
        // arrange
        using var factory = new RecamApiFactory();
        var viewer = await factory.PairDeviceAsync(DeviceRole.Viewer);
        using var client = factory.CreateDeviceClient(viewer.Credential);

        // act
        using var response = await client.PutAsJsonAsync(QuotaUri, new { quotaMb = 300 }, ApiJson.Options, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var quota = await client.GetFromJsonAsync<QuotaResponse>(QuotaUri, ApiJson.Options, TestContext.Current.CancellationToken);
        Assert.Equal(300, quota!.QuotaMb);
    }

    [Fact(DisplayName = "A quota larger than the disk is refused")]
    public async Task PutQuota_LargerThanUsedPlusFree_Returns400()
    {
        // arrange
        using var factory = new RecamApiFactory();
        var owner = await factory.PairDeviceAsync(DeviceRole.Owner);
        using var client = factory.CreateDeviceClient(owner.Credential);

        // act
        using var response = await client.PutAsJsonAsync(QuotaUri, new { quotaMb = int.MaxValue }, ApiJson.Options, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(TestContext.Current.CancellationToken);
        Assert.Contains("quotaMb", problem!.Errors.Keys);
    }

    [Theory(DisplayName = "A missing or tiny quota is refused")]
    [InlineData(null)]
    [InlineData(99)]
    public async Task PutQuota_BelowMinimum_Returns400(int? quotaMb)
    {
        // arrange
        using var factory = new RecamApiFactory();
        var owner = await factory.PairDeviceAsync(DeviceRole.Owner);
        using var client = factory.CreateDeviceClient(owner.Credential);

        // act
        using var response = await client.PutAsJsonAsync(QuotaUri, new { quotaMb }, ApiJson.Options, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact(DisplayName = "A camera cannot touch the quota")]
    public async Task GetQuota_AsCamera_Returns403()
    {
        // arrange
        using var factory = new RecamApiFactory();
        var camera = await factory.PairDeviceAsync(DeviceRole.Camera);
        using var client = factory.CreateDeviceClient(camera.Credential);

        // act
        using var response = await client.GetAsync(QuotaUri, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
