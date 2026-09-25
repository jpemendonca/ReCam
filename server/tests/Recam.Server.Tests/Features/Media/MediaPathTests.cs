using System.Net;
using Microsoft.EntityFrameworkCore;
using Recam.Server.Domain;
using Recam.Server.Tests.Support;

namespace Recam.Server.Tests.Features.Media;

public sealed class MediaPathTests
{
    [Fact(DisplayName = "A camera without recording publishes to its cam- path")]
    public async Task Whip_WhenNotRecording_GoesToCamPath()
    {
        // arrange
        using var mediaMtx = new FakeMediaMtx();
        using var factory = new RecamApiFactory { MediaMtxUrl = mediaMtx.Url };
        var camera = await factory.PairDeviceAsync(DeviceRole.Camera);
        using var client = factory.CreateDeviceClient(camera.Credential);

        // act
        using var response = await client.PostAsync(WhipUri(camera.DeviceId), Sdp(), TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal($"POST /cam-{camera.DeviceId:N}/whip", Assert.Single(mediaMtx.Requests));
        Assert.Equal($"/whip/{camera.DeviceId}/cam-{FakeMediaMtx.SessionId}", response.Headers.Location?.OriginalString);
    }

    [Fact(DisplayName = "A recording camera publishes to its rec- path, and viewers read from it")]
    public async Task WhipAndWhep_WhenRecording_GoToRecPath()
    {
        // arrange
        using var mediaMtx = new FakeMediaMtx();
        using var factory = new RecamApiFactory { MediaMtxUrl = mediaMtx.Url };
        var owner = await factory.PairDeviceAsync(DeviceRole.Owner);
        var camera = await factory.PairDeviceAsync(DeviceRole.Camera);
        await SetRecordingAsync(factory, camera.DeviceId, recording: true);
        using var cameraClient = factory.CreateDeviceClient(camera.Credential);
        using var viewerClient = factory.CreateDeviceClient(owner.Credential);

        // act
        using var published = await cameraClient.PostAsync(WhipUri(camera.DeviceId), Sdp(), TestContext.Current.CancellationToken);
        using var watched = await viewerClient.PostAsync(
            new Uri($"/whep/{camera.DeviceId}", UriKind.Relative), Sdp(), TestContext.Current.CancellationToken);

        // assert
        Assert.Equal([$"POST /rec-{camera.DeviceId:N}/whip", $"POST /rec-{camera.DeviceId:N}/whep"], mediaMtx.Requests);
        Assert.Equal($"/whip/{camera.DeviceId}/rec-{FakeMediaMtx.SessionId}", published.Headers.Location?.OriginalString);
        Assert.Equal($"/whep/{camera.DeviceId}/rec-{FakeMediaMtx.SessionId}", watched.Headers.Location?.OriginalString);
    }

    [Fact(DisplayName = "Ending a session goes to the path it started on, even after recording changed")]
    public async Task WhipDelete_AfterRecordingTurnedOff_UsesTheSessionPath()
    {
        // arrange
        using var mediaMtx = new FakeMediaMtx();
        using var factory = new RecamApiFactory { MediaMtxUrl = mediaMtx.Url };
        var camera = await factory.PairDeviceAsync(DeviceRole.Camera);
        await SetRecordingAsync(factory, camera.DeviceId, recording: true);
        using var client = factory.CreateDeviceClient(camera.Credential);
        using var published = await client.PostAsync(WhipUri(camera.DeviceId), Sdp(), TestContext.Current.CancellationToken);
        await SetRecordingAsync(factory, camera.DeviceId, recording: false);

        // act
        using var ended = await client.DeleteAsync(published.Headers.Location, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal($"DELETE /rec-{camera.DeviceId:N}/whip/{FakeMediaMtx.SessionId}", mediaMtx.Requests.Last());
    }

    [Fact(DisplayName = "A session the proxy did not hand out is not found")]
    public async Task WhipPatch_WithUnknownSession_Returns404()
    {
        // arrange
        using var mediaMtx = new FakeMediaMtx();
        using var factory = new RecamApiFactory { MediaMtxUrl = mediaMtx.Url };
        var camera = await factory.PairDeviceAsync(DeviceRole.Camera);
        using var client = factory.CreateDeviceClient(camera.Credential);
        using var request = new HttpRequestMessage(HttpMethod.Patch, new Uri($"/whip/{camera.DeviceId}/{FakeMediaMtx.SessionId}", UriKind.Relative))
        {
            Content = Sdp(),
        };

        // act
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Empty(mediaMtx.Requests);
    }

    private static Uri WhipUri(Guid cameraId) => new($"/whip/{cameraId}", UriKind.Relative);

    private static StringContent Sdp() => new("v=0\r\n", System.Text.Encoding.UTF8, "application/sdp");

    private static async Task SetRecordingAsync(RecamApiFactory factory, Guid cameraId, bool recording)
    {
        await using var database = await factory.CreateDatabaseAsync();
        await database.Devices
            .Where(device => device.Id == cameraId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(device => device.RecordingEnabled, recording), TestContext.Current.CancellationToken);
    }
}
