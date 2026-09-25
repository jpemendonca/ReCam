using System.Net;
using System.Net.Http.Headers;
using Recam.Server.Domain;
using Recam.Server.Tests.Support;

namespace Recam.Server.Tests.Features.Media;

public sealed class MediaProxyEndpointsTests(MediaMtxFixture mediaMtx) : IClassFixture<MediaMtxFixture>
{
    private const string MinimalOffer = "v=0\r\no=- 0 0 IN IP4 127.0.0.1\r\ns=-\r\nt=0 0\r\n";

    private static StringContent Sdp(string body)
    {
        var content = new StringContent(body);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/sdp");
        return content;
    }

    [Fact(DisplayName = "Watching without a credential is refused before reaching MediaMTX")]
    public async Task Whep_WithoutCredential_Returns401()
    {
        // arrange
        using var factory = new RecamApiFactory { MediaMtxUrl = mediaMtx.Url };
        using var client = factory.CreateClient();

        // act
        using var response = await client.PostAsync(
            new Uri($"/whep/{Guid.NewGuid()}", UriKind.Relative), Sdp(MinimalOffer), TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact(DisplayName = "A camera cannot publish on another camera's stream")]
    public async Task Whip_ToOtherCameraPath_Returns403()
    {
        // arrange
        using var factory = new RecamApiFactory { MediaMtxUrl = mediaMtx.Url };
        var camera = await factory.PairDeviceAsync(DeviceRole.Camera);
        var otherCamera = await factory.PairDeviceAsync(DeviceRole.Camera);
        using var client = factory.CreateDeviceClient(camera.Credential);

        // act
        using var response = await client.PostAsync(
            new Uri($"/whip/{otherCamera.DeviceId}", UriKind.Relative), Sdp(MinimalOffer), TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact(DisplayName = "Watching a camera that is not publishing gets MediaMTX's not found through the proxy")]
    public async Task Whep_ForCameraNotPublishing_ReturnsUpstream404()
    {
        // arrange
        using var factory = new RecamApiFactory { MediaMtxUrl = mediaMtx.Url };
        var owner = await factory.PairDeviceAsync(DeviceRole.Owner);
        var camera = await factory.PairDeviceAsync(DeviceRole.Camera);
        using var client = factory.CreateDeviceClient(owner.Credential);

        // act
        using var response = await client.PostAsync(
            new Uri($"/whep/{camera.DeviceId}", UriKind.Relative), Sdp(MinimalOffer), TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact(DisplayName = "A camera's publish offer reaches MediaMTX, which rejects a bogus SDP")]
    public async Task Whip_WithBogusOffer_ReachesMediaMtx()
    {
        // arrange
        using var factory = new RecamApiFactory { MediaMtxUrl = mediaMtx.Url };
        var camera = await factory.PairDeviceAsync(DeviceRole.Camera);
        using var client = factory.CreateDeviceClient(camera.Credential);

        // act
        using var response = await client.PostAsync(
            new Uri($"/whip/{camera.DeviceId}", UriKind.Relative), Sdp("not an sdp"), TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact(DisplayName = "Tests use the same MediaMTX image as both compose files")]
    public void MediaMtxImage_MatchesComposeFiles()
    {
        // arrange
        string[] composeFiles = ["deploy/compose.yaml", "deploy/compose.bridge.yaml"];

        // act
        var contents = composeFiles.Select(file => File.ReadAllText(MediaMtxFixture.RepositoryFile(file))).ToList();

        // assert
        Assert.All(contents, content => Assert.Contains($"image: {MediaMtxFixture.Image}", content, StringComparison.Ordinal));
    }
}
