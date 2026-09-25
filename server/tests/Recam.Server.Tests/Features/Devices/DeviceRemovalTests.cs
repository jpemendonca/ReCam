using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;
using Recam.Server.Domain;
using Recam.Server.Features.Devices;
using Recam.Server.Tests.Support;

namespace Recam.Server.Tests.Features.Devices;

public sealed class DeviceRemovalTests
{
    private static readonly TimeSpan Wait = TimeSpan.FromSeconds(10);
    private static readonly Uri DevicesUri = new("/api/devices", UriKind.Relative);

    [Fact(DisplayName = "A Monitor removes a camera: revoked, disconnected and gone from the lists")]
    public async Task DeleteDevice_AsMonitor_RevokesAndDisconnects()
    {
        // arrange
        using var factory = new RecamApiFactory();
        var owner = await factory.PairDeviceAsync(DeviceRole.Owner);
        var camera = await factory.PairDeviceAsync(DeviceRole.Camera, "Porch");
        await using var viewerConnection = await factory.ConnectAsync(owner.Credential);
        var removed = new TaskCompletionSource<Guid>();
        viewerConnection.On<Guid>("CameraRemoved", id => removed.TrySetResult(id));
        await using var cameraConnection = await factory.ConnectAsync(camera.Credential);
        var closed = new TaskCompletionSource();
        cameraConnection.Closed += _ =>
        {
            closed.TrySetResult();
            return Task.CompletedTask;
        };
        using var ownerClient = factory.CreateDeviceClient(owner.Credential);

        // act
        using var response = await ownerClient.DeleteAsync(new Uri($"/api/devices/{camera.DeviceId}", UriKind.Relative), TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await closed.Task.WaitAsync(Wait, TestContext.Current.CancellationToken);
        Assert.Equal(camera.DeviceId, await removed.Task.WaitAsync(Wait, TestContext.Current.CancellationToken));
        using var cameraClient = factory.CreateDeviceClient(camera.Credential);
        using var me = await cameraClient.GetAsync(new Uri("/api/me", UriKind.Relative), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
        var cameras = await ownerClient.GetFromJsonAsync<List<CameraStatus>>(
            new Uri("/api/cameras", UriKind.Relative), ApiJson.Options, TestContext.Current.CancellationToken);
        Assert.Empty(cameras!);
    }

    [Fact(DisplayName = "The device list shows cameras first, then Monitors, with presence")]
    public async Task GetDevices_AsMonitor_ListsEveryActiveDevice()
    {
        // arrange
        using var factory = new RecamApiFactory();
        var owner = await factory.PairDeviceAsync(DeviceRole.Owner, "Redmi 6A");
        var camera = await factory.PairDeviceAsync(DeviceRole.Camera, "Samsung A10");
        await using var cameraConnection = await factory.ConnectAsync(camera.Credential);
        using var client = factory.CreateDeviceClient(owner.Credential);

        // act
        var devices = await client.GetFromJsonAsync<List<DeviceResponse>>(DevicesUri, ApiJson.Options, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(
            [new DeviceResponse(camera.DeviceId, "Samsung A10", DeviceRole.Camera, true), new DeviceResponse(owner.DeviceId, "Redmi 6A", DeviceRole.Owner, false)],
            devices);
    }

    [Fact(DisplayName = "A Monitor cannot remove itself from the list")]
    public async Task DeleteDevice_Itself_Returns409()
    {
        // arrange
        using var factory = new RecamApiFactory();
        var owner = await factory.PairDeviceAsync(DeviceRole.Owner);
        using var client = factory.CreateDeviceClient(owner.Credential);

        // act
        using var response = await client.DeleteAsync(new Uri($"/api/devices/{owner.DeviceId}", UriKind.Relative), TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact(DisplayName = "Removing a device that does not exist is not found")]
    public async Task DeleteDevice_Unknown_Returns404()
    {
        // arrange
        using var factory = new RecamApiFactory();
        var owner = await factory.PairDeviceAsync(DeviceRole.Owner);
        using var client = factory.CreateDeviceClient(owner.Credential);

        // act
        using var response = await client.DeleteAsync(new Uri($"/api/devices/{Guid.NewGuid()}", UriKind.Relative), TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact(DisplayName = "A camera cannot remove devices")]
    public async Task DeleteDevice_AsCamera_Returns403()
    {
        // arrange
        using var factory = new RecamApiFactory();
        var owner = await factory.PairDeviceAsync(DeviceRole.Owner);
        var camera = await factory.PairDeviceAsync(DeviceRole.Camera);
        using var client = factory.CreateDeviceClient(camera.Credential);

        // act
        using var response = await client.DeleteAsync(new Uri($"/api/devices/{owner.DeviceId}", UriKind.Relative), TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
