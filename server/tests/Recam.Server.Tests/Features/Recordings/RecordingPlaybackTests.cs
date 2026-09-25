using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;
using Recam.Server.Domain;
using Recam.Server.Features.Realtime;
using Recam.Server.Features.Recordings;
using Recam.Server.Infrastructure.Realtime;
using Recam.Server.Tests.Support;

namespace Recam.Server.Tests.Features.Recordings;

public sealed class RecordingPlaybackTests
{
    private static readonly DateTimeOffset TenOClock = new(2026, 9, 24, 10, 0, 0, TimeSpan.Zero);

    [Fact(DisplayName = "A day without recordings lists nothing")]
    public async Task ListRecordings_EmptyDay_ReturnsEmptyList()
    {
        // arrange
        using var factory = new RecamApiFactory();
        var (client, camera) = await MonitorAndCameraAsync(factory);
        using var _ = client;

        // act
        var pieces = await ListAsync(client, camera, "2026-09-24");

        // assert
        Assert.Empty(pieces);
    }

    [Fact(DisplayName = "Back-to-back segments come as one stretch with a playable url each")]
    public async Task ListRecordings_WithSegments_JoinsThem()
    {
        // arrange
        using var factory = new RecamApiFactory();
        var (client, camera) = await MonitorAndCameraAsync(factory);
        using var _ = client;
        RecordingFiles.Write(factory.RecordingsDirectory, camera, TenOClock, 100);
        RecordingFiles.Write(factory.RecordingsDirectory, camera, TenOClock.AddMinutes(1), 100);
        RecordingFiles.Write(factory.RecordingsDirectory, camera, TenOClock.AddHours(2), 100);

        // act
        var pieces = await ListAsync(client, camera, "2026-09-24");

        // assert
        Assert.Equal(2, pieces.Count);
        Assert.Equal(TenOClock, pieces[0].Start);
        Assert.Equal(TenOClock.AddMinutes(2), pieces[0].End);
        Assert.Equal(
            $"/api/recordings/{camera}/2026-09-24_10-00-00-000000.mp4",
            pieces[0].Segments[0].Url);
        var days = await client.GetFromJsonAsync<List<string>>(
            new Uri($"/api/cameras/{camera}/recording-days", UriKind.Relative), ApiJson.Options, TestContext.Current.CancellationToken);
        Assert.Equal(["2026-09-24"], days);
    }

    [Fact(DisplayName = "The segment a recording camera is still writing is left out")]
    public async Task ListRecordings_WhileRecording_LeavesOutTheGrowingSegment()
    {
        // arrange
        using var factory = new RecamApiFactory();
        var owner = await factory.PairDeviceAsync(DeviceRole.Owner);
        var camera = await factory.PairDeviceAsync(DeviceRole.Camera);
        await using var ownerConnection = await factory.ConnectAsync(owner.Credential);
        await using var cameraConnection = await factory.ConnectAsync(camera.Credential);
        await ownerConnection.InvokeAsync<HubResult>("SetRecording", camera.DeviceId, true, TestContext.Current.CancellationToken);
        await cameraConnection.InvokeAsync<HubResult>("ReportPublishing", true, TestContext.Current.CancellationToken);
        RecordingFiles.Write(factory.RecordingsDirectory, camera.DeviceId, TenOClock, 100);
        RecordingFiles.Write(factory.RecordingsDirectory, camera.DeviceId, TenOClock.AddMinutes(1), 100);
        using var client = factory.CreateDeviceClient(owner.Credential);

        // act
        var pieces = await ListAsync(client, camera.DeviceId, "2026-09-24");

        // assert
        var piece = Assert.Single(pieces);
        Assert.Single(piece.Segments);
    }

    [Fact(DisplayName = "A day in another format is refused")]
    public async Task ListRecordings_WithBadDay_Returns400()
    {
        // arrange
        using var factory = new RecamApiFactory();
        var (client, camera) = await MonitorAndCameraAsync(factory);
        using var _ = client;

        // act
        using var response = await client.GetAsync(
            new Uri($"/api/cameras/{camera}/recordings?day=24/09/2026", UriKind.Relative), TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact(DisplayName = "A segment is served in ranges, so a player can seek")]
    public async Task ServeSegment_WithRange_ReturnsPartialContent()
    {
        // arrange
        using var factory = new RecamApiFactory();
        var (client, camera) = await MonitorAndCameraAsync(factory);
        using var _ = client;
        var path = RecordingFiles.Write(factory.RecordingsDirectory, camera, TenOClock, 0);
        await File.WriteAllBytesAsync(path, [.. Enumerable.Range(0, 100).Select(value => (byte)value)], TestContext.Current.CancellationToken);
        using var request = new HttpRequestMessage(
            HttpMethod.Get, new Uri($"/api/recordings/{camera}/2026-09-24_10-00-00-000000.mp4", UriKind.Relative));
        request.Headers.Range = new RangeHeaderValue(10, 19);

        // act
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.PartialContent, response.StatusCode);
        Assert.Equal("video/mp4", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(Enumerable.Range(10, 10).Select(value => (byte)value), await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken));
    }

    [Theory(DisplayName = "A name that is not a recording never reaches the disk")]
    [InlineData("..%2F..%2Frecam.db")]
    [InlineData("%2E%2E")]
    [InlineData("secret.txt")]
    [InlineData("2026-09-24_10-00-00-000000.mp4.bak")]
    public async Task ServeSegment_WithMaliciousName_Returns404(string segment)
    {
        // arrange
        using var factory = new RecamApiFactory();
        var (client, camera) = await MonitorAndCameraAsync(factory);
        using var _ = client;
        await File.WriteAllTextAsync(Path.Combine(factory.DataDirectory, "secret.txt"), "x", TestContext.Current.CancellationToken);

        // act
        using var response = await client.GetAsync(new Uri($"/api/recordings/{camera}/{segment}", UriKind.Relative), TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact(DisplayName = "A camera cannot read recordings")]
    public async Task ListRecordings_AsCamera_Returns403()
    {
        // arrange
        using var factory = new RecamApiFactory();
        var camera = await factory.PairDeviceAsync(DeviceRole.Camera);
        using var client = factory.CreateDeviceClient(camera.Credential);

        // act
        using var response = await client.GetAsync(
            new Uri($"/api/cameras/{camera.DeviceId}/recordings?day=2026-09-24", UriKind.Relative), TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static async Task<(HttpClient Client, Guid Camera)> MonitorAndCameraAsync(RecamApiFactory factory)
    {
        var owner = await factory.PairDeviceAsync(DeviceRole.Owner);
        var camera = await factory.PairDeviceAsync(DeviceRole.Camera);
        return (factory.CreateDeviceClient(owner.Credential), camera.DeviceId);
    }

    private static async Task<List<RecordingPieceResponse>> ListAsync(HttpClient client, Guid camera, string day) =>
        (await client.GetFromJsonAsync<List<RecordingPieceResponse>>(
            new Uri($"/api/cameras/{camera}/recordings?day={day}", UriKind.Relative), ApiJson.Options, TestContext.Current.CancellationToken))!;
}
