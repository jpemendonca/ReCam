using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Recam.Server.Domain;
using Recam.Server.Features.Pairing;
using Recam.Server.Tests.Support;

namespace Recam.Server.Tests.Features.Pairing;

public sealed class PairingEndpointsTests
{
    [Fact(DisplayName = "Pairing with the owner token creates the owner and returns its credential")]
    public async Task Pair_WithOwnerToken_CreatesOwner()
    {
        // arrange
        using var factory = new RecamApiFactory();
        var token = await factory.CreatePairingTokenAsync(DeviceRole.Owner);
        using var client = factory.CreateClient();

        // act
        using var response = await client.PairAsync(token, "  My phone ");

        // assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<PairResponse>(ApiJson.Options, TestContext.Current.CancellationToken);
        Assert.NotNull(body);
        Assert.Equal(DeviceRole.Owner, body.Role);
        Assert.StartsWith($"{body.DeviceId:N}.", body.Credential, StringComparison.Ordinal);
        await using var database = await factory.CreateDatabaseAsync();
        var device = await database.Devices.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal("My phone", device.Name);
        Assert.Equal(DeviceRole.Owner, device.Role);
        Assert.NotEqual(body.Credential, Convert.ToHexString(device.CredentialHash));
    }

    [Fact(DisplayName = "Pairing with an already used token is rejected")]
    public async Task Pair_WithUsedToken_Returns401()
    {
        // arrange
        using var factory = new RecamApiFactory();
        var token = await factory.CreatePairingTokenAsync(DeviceRole.Camera);
        using var client = factory.CreateClient();
        using var first = await client.PairAsync(token, "Kitchen");

        // act
        using var response = await client.PairAsync(token, "Kitchen again");

        // assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.Current.CancellationToken);
        Assert.Equal(PairingErrors.InvalidToken.Code, problem?.Extensions["code"]?.ToString());
    }

    [Fact(DisplayName = "Pairing in the wrong tab is refused and the token still works in the right one")]
    public async Task Pair_WithWrongExpectedRole_Returns409AndKeepsToken()
    {
        // arrange
        using var factory = new RecamApiFactory();
        var token = await factory.CreatePairingTokenAsync(DeviceRole.Owner);
        using var client = factory.CreateClient();

        // act
        using var wrongTab = await client.PairAsync(token, "Old phone", [DeviceRole.Camera]);
        using var rightTab = await client.PairAsync(token, "New phone", [DeviceRole.Owner, DeviceRole.Viewer]);

        // assert
        Assert.Equal(HttpStatusCode.Conflict, wrongTab.StatusCode);
        var problem = await wrongTab.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.Current.CancellationToken);
        Assert.Equal(PairingErrors.WrongRole.Code, problem?.Extensions["code"]?.ToString());
        Assert.Equal(HttpStatusCode.Created, rightTab.StatusCode);
        await using var database = await factory.CreateDatabaseAsync();
        Assert.Equal("New phone", (await database.Devices.SingleAsync(TestContext.Current.CancellationToken)).Name);
    }

    [Fact(DisplayName = "Pairing with an expired token is rejected with the same answer as any bad token")]
    public async Task Pair_WithExpiredToken_Returns401()
    {
        // arrange
        using var factory = new RecamApiFactory();
        var token = await factory.CreatePairingTokenAsync(DeviceRole.Camera);
        factory.Time.Advance(PairingToken.Lifetime);
        using var client = factory.CreateClient();

        // act
        using var response = await client.PairAsync(token, "Garage");

        // assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.Current.CancellationToken);
        Assert.Equal(PairingErrors.InvalidToken.Code, problem?.Extensions["code"]?.ToString());
    }

    [Fact(DisplayName = "Pairing with an unknown token is rejected")]
    public async Task Pair_WithUnknownToken_Returns401()
    {
        // arrange
        using var factory = new RecamApiFactory();
        using var client = factory.CreateClient();

        // act
        using var response = await client.PairAsync(SecretToken.Generate(), "Garage");

        // assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact(DisplayName = "Invalid pairing input reports every invalid field at once")]
    public async Task Pair_WithInvalidName_ReturnsAllValidationErrors()
    {
        // arrange
        using var factory = new RecamApiFactory();
        using var client = factory.CreateClient();

        // act
        using var response = await client.PairAsync(token: "", name: new string('x', 41));

        // assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(TestContext.Current.CancellationToken);
        Assert.NotNull(problem);
        Assert.Equal(["name", "token"], problem.Errors.Keys.Order(StringComparer.Ordinal));
    }

    [Fact(DisplayName = "More than five pairing attempts per minute from one address are throttled")]
    public async Task Pair_AboveRateLimit_Returns429()
    {
        // arrange
        using var factory = new RecamApiFactory();
        using var client = factory.CreateClient();
        for (var attempt = 0; attempt < 5; attempt++)
        {
            using var allowed = await client.PairAsync(SecretToken.Generate(), "Probe");
        }

        // act
        using var response = await client.PairAsync(SecretToken.Generate(), "Probe");

        // assert
        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
    }
}
