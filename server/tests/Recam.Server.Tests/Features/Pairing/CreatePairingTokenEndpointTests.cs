using System.Net;
using System.Net.Http.Json;
using System.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Recam.Server.Domain;
using Recam.Server.Features.Pairing;
using Recam.Server.Tests.Support;

namespace Recam.Server.Tests.Features.Pairing;

public sealed class CreatePairingTokenEndpointTests
{
    private static readonly Uri TokensUri = new("/api/pairing-tokens", UriKind.Relative);

    [Fact(DisplayName = "The owner gets a QR URI whose token pairs a camera")]
    public async Task CreatePairingToken_AsOwner_ReturnsQrUri()
    {
        // arrange
        using var factory = new RecamApiFactory();
        var owner = await factory.PairDeviceAsync(DeviceRole.Owner);
        using var ownerClient = factory.CreateDeviceClient(owner.Credential);

        // act
        using var response = await ownerClient.PostAsJsonAsync(TokensUri, new { role = "camera" }, ApiJson.Options, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CreatePairingTokenResponse>(ApiJson.Options, TestContext.Current.CancellationToken);
        Assert.NotNull(body);
        Assert.Equal(factory.Time.GetUtcNow().Add(PairingToken.Lifetime), body.ExpiresAt);
        var query = HttpUtility.ParseQueryString(new Uri(body.QrUri).Query);
        Assert.Equal("1", query["v"]);
        Assert.Equal(64, query["f"]?.Length);

        using var cameraClient = factory.CreateClient();
        using var paired = await cameraClient.PairAsync(query["t"], "Kitchen");
        var camera = await paired.Content.ReadFromJsonAsync<PairResponse>(ApiJson.Options, TestContext.Current.CancellationToken);
        Assert.Equal(DeviceRole.Camera, camera?.Role);
        await using var database = await factory.CreateDatabaseAsync();
        var token = await database.PairingTokens.SingleAsync(candidate => candidate.CreatedByDeviceId == owner.DeviceId, TestContext.Current.CancellationToken);
        Assert.Equal(DeviceRole.Camera, token.GrantsRole);
    }

    [Fact(DisplayName = "A camera cannot create pairing tokens")]
    public async Task CreatePairingToken_AsCamera_Returns403()
    {
        // arrange
        using var factory = new RecamApiFactory();
        var camera = await factory.PairDeviceAsync(DeviceRole.Camera);
        using var client = factory.CreateDeviceClient(camera.Credential);

        // act
        using var response = await client.PostAsJsonAsync(TokensUri, new { role = "camera" }, ApiJson.Options, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact(DisplayName = "Creating a pairing token requires a credential")]
    public async Task CreatePairingToken_WithoutCredential_Returns401()
    {
        // arrange
        using var factory = new RecamApiFactory();
        using var client = factory.CreateClient();

        // act
        using var response = await client.PostAsJsonAsync(TokensUri, new { role = "camera" }, ApiJson.Options, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory(DisplayName = "Roles other than camera are rejected with a validation problem")]
    [InlineData("owner")]
    [InlineData("nonsense")]
    [InlineData(null)]
    public async Task CreatePairingToken_WithUnsupportedRole_Returns400(string? role)
    {
        // arrange
        using var factory = new RecamApiFactory();
        var owner = await factory.PairDeviceAsync(DeviceRole.Owner);
        using var client = factory.CreateDeviceClient(owner.Credential);

        // act
        using var response = await client.PostAsJsonAsync(TokensUri, new { role }, ApiJson.Options, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(TestContext.Current.CancellationToken);
        Assert.Contains("role", problem!.Errors.Keys);
    }
}
