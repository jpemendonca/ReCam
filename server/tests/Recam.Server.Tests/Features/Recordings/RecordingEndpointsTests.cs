using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Recam.Server.Domain;
using Recam.Server.Features.Recordings;
using Recam.Server.Tests.Support;

namespace Recam.Server.Tests.Features.Recordings;

public sealed class RecordingEndpointsTests
{
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
