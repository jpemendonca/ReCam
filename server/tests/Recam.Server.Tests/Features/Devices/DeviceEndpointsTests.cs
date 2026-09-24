using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Recam.Server.Domain;
using Recam.Server.Features.Devices;
using Recam.Server.Tests.Support;

namespace Recam.Server.Tests.Features.Devices;

public sealed class DeviceEndpointsTests
{
    private static readonly Uri MeUri = new("/api/me", UriKind.Relative);

    [Fact(DisplayName = "A paired device can read who it is")]
    public async Task Me_WithValidCredential_ReturnsDevice()
    {
        // arrange
        using var factory = new RecamApiFactory();
        var paired = await factory.PairDeviceAsync(DeviceRole.Camera, "Porch");
        using var client = factory.CreateDeviceClient(paired.Credential);

        // act
        var me = await client.GetFromJsonAsync<MeResponse>(MeUri, ApiJson.Options, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(new MeResponse(paired.DeviceId, "Porch", DeviceRole.Camera), me);
    }

    [Fact(DisplayName = "A revoked device is no longer authenticated")]
    public async Task Me_WithRevokedDevice_Returns401()
    {
        // arrange
        using var factory = new RecamApiFactory();
        var paired = await factory.PairDeviceAsync(DeviceRole.Viewer);
        await using (var database = await factory.CreateDatabaseAsync())
        {
            await database.Devices.ExecuteUpdateAsync(
                setters => setters.SetProperty(device => device.RevokedAt, factory.Time.GetUtcNow()),
                TestContext.Current.CancellationToken);
        }

        using var client = factory.CreateDeviceClient(paired.Credential);

        // act
        using var response = await client.GetAsync(MeUri, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact(DisplayName = "A credential with the wrong secret is rejected")]
    public async Task Me_WithWrongSecret_Returns401()
    {
        // arrange
        using var factory = new RecamApiFactory();
        var paired = await factory.PairDeviceAsync(DeviceRole.Owner);
        using var client = factory.CreateDeviceClient(DeviceCredential.Format(paired.DeviceId, SecretToken.Generate()));

        // act
        using var response = await client.GetAsync(MeUri, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact(DisplayName = "Requests without a credential are rejected")]
    public async Task Me_WithoutCredential_Returns401()
    {
        // arrange
        using var factory = new RecamApiFactory();
        using var client = factory.CreateClient();

        // act
        using var response = await client.GetAsync(MeUri, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
