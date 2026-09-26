using Recam.Web.Api;
using Recam.Web.Cameras;
using Recam.Web.Tests.Support;

namespace Recam.Web.Tests.Cameras;

public sealed class CameraListControllerTests
{
    private readonly FakeRecamApi _api = new() { Me = new MeInfo(Guid.NewGuid(), "Navegador", "owner") };
    private readonly FakeDeviceHub _hub = new();

    [Fact(DisplayName = "Starting connects the hub and loads the cameras sorted by name")]
    public async Task Start_LoadsSortedCameras()
    {
        // arrange
        _api.Cameras.AddRange([Support.Cameras.Make("Quintal"), Support.Cameras.Make("Garagem")]);
        using var controller = new CameraListController(_api, _hub);

        // act
        await controller.StartAsync(CancellationToken.None);

        // assert
        Assert.True(controller.Connected);
        Assert.Equal(CameraListState.Loaded, controller.State);
        Assert.Equal(["Garagem", "Quintal"], controller.Cameras.Select(camera => camera.Name));
    }

    [Fact(DisplayName = "A status from the hub replaces that camera in the list")]
    public async Task StatusChanged_ReplacesCamera()
    {
        // arrange
        var camera = Support.Cameras.Make("Porta", online: false);
        _api.Cameras.Add(camera);
        using var controller = new CameraListController(_api, _hub);
        await controller.StartAsync(CancellationToken.None);

        // act
        _hub.SendStatus(camera with { Online = true, Publishing = true });

        // assert
        var shown = Assert.Single(controller.Cameras);
        Assert.True(shown.Online);
        Assert.True(shown.Publishing);
    }

    [Fact(DisplayName = "A removed camera leaves the list")]
    public async Task CameraRemoved_LeavesList()
    {
        // arrange
        var camera = Support.Cameras.Make("Porta");
        _api.Cameras.Add(camera);
        using var controller = new CameraListController(_api, _hub);
        await controller.StartAsync(CancellationToken.None);

        // act
        _hub.SendRemoved(camera.Id);

        // assert
        Assert.Empty(controller.Cameras);
    }

    [Fact(DisplayName = "When the hub comes back, the list reloads what changed meanwhile")]
    public async Task Reconnected_ReloadsList()
    {
        // arrange
        using var controller = new CameraListController(_api, _hub);
        await controller.StartAsync(CancellationToken.None);
        _hub.Drop();
        _api.Cameras.Add(Support.Cameras.Make("Nova"));

        // act
        _hub.Reconnect();

        // assert
        Assert.Equal("Nova", Assert.Single(controller.Cameras).Name);
    }

    [Fact(DisplayName = "A browser the server no longer knows is signed out")]
    public async Task Start_Unknown_SignedOut()
    {
        // arrange
        _api.Me = null;
        using var controller = new CameraListController(_api, _hub);

        // act
        await controller.StartAsync(CancellationToken.None);

        // assert
        Assert.Equal(CameraListState.SignedOut, controller.State);
    }

    [Fact(DisplayName = "Without the server the first load fails, and retrying loads it")]
    public async Task Start_Offline_FailsThenRetries()
    {
        // arrange
        _api.Offline = true;
        _api.Cameras.Add(Support.Cameras.Make("Porta"));
        using var controller = new CameraListController(_api, _hub);
        await controller.StartAsync(CancellationToken.None);
        var failed = controller.State;
        _api.Offline = false;

        // act
        await controller.RetryAsync(CancellationToken.None);

        // assert
        Assert.Equal(CameraListState.Failed, failed);
        Assert.Equal(CameraListState.Loaded, controller.State);
    }

    [Fact(DisplayName = "Record always goes through the hub and reports a refusal")]
    public async Task SetRecording_Refused_ReturnsFalse()
    {
        // arrange
        var camera = Support.Cameras.Make("Porta");
        _api.Cameras.Add(camera);
        using var controller = new CameraListController(_api, _hub);
        await controller.StartAsync(CancellationToken.None);
        _hub.RecordingAccepted = false;

        // act
        var accepted = await controller.SetRecordingAsync(camera.Id, enabled: true);

        // assert
        Assert.False(accepted);
        Assert.Contains($"SetRecording {camera.Id} True", _hub.Calls);
    }
}
