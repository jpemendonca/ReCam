using Recam.Web.Api;
using Recam.Web.Start;
using Recam.Web.Tests.Support;

namespace Recam.Web.Tests.Start;

public sealed class StartControllerTests
{
    [Fact(DisplayName = "On a server without Monitor, the browser asks for the first-time code")]
    public async Task Load_NoMonitor_NeedsCode()
    {
        // arrange
        var controller = new StartController(new FakeRecamApi());

        // act
        await controller.LoadAsync(CancellationToken.None);

        // assert
        Assert.Equal(StartState.NeedsCode, controller.State);
    }

    [Fact(DisplayName = "A browser that is already the Monitor goes straight in")]
    public async Task Load_AlreadyMonitor_IsMonitor()
    {
        // arrange
        var api = new FakeRecamApi { Me = new MeInfo(Guid.NewGuid(), "Navegador", "owner") };
        var controller = new StartController(api);

        // act
        await controller.LoadAsync(CancellationToken.None);

        // assert
        Assert.Equal(StartState.Monitor, controller.State);
        Assert.Equal("Navegador", controller.Me?.Name);
    }

    [Fact(DisplayName = "When another device is the Monitor, the browser says so")]
    public async Task Load_OtherMonitor_ServerTaken()
    {
        // arrange
        var controller = new StartController(new FakeRecamApi { OtherMonitor = true });

        // act
        await controller.LoadAsync(CancellationToken.None);

        // assert
        Assert.Equal(StartState.ServerTaken, controller.State);
    }

    [Fact(DisplayName = "The right code makes the browser the Monitor and sends the remember choice")]
    public async Task SubmitCode_Right_BecomesMonitor()
    {
        // arrange
        var api = new FakeRecamApi();
        var controller = new StartController(api);
        await controller.LoadAsync(CancellationToken.None);

        // act
        await controller.SubmitCodeAsync(FakeRecamApi.Code, remember: false, CancellationToken.None);

        // assert
        Assert.Equal(StartState.Monitor, controller.State);
        Assert.Null(controller.CodeError);
        Assert.Equal([false], api.RememberSent);
    }

    [Fact(DisplayName = "A wrong code keeps the form, with the reason")]
    public async Task SubmitCode_Wrong_ShowsError()
    {
        // arrange
        var controller = new StartController(new FakeRecamApi());
        await controller.LoadAsync(CancellationToken.None);

        // act
        await controller.SubmitCodeAsync("ZZZZ-ZZZZ", remember: true, CancellationToken.None);

        // assert
        Assert.Equal(StartState.NeedsCode, controller.State);
        Assert.Equal(FirstOpenOutcome.WrongCode, controller.CodeError);
    }

    [Fact(DisplayName = "Signing out brings the code form back")]
    public async Task SignOut_Monitor_NeedsCodeAgain()
    {
        // arrange
        var api = new FakeRecamApi { Me = new MeInfo(Guid.NewGuid(), "Navegador", "owner") };
        var controller = new StartController(api);
        await controller.LoadAsync(CancellationToken.None);

        // act
        await controller.SignOutAsync(CancellationToken.None);

        // assert
        Assert.Equal(StartState.NeedsCode, controller.State);
        Assert.Null(controller.Me);
    }

    [Fact(DisplayName = "Without the server, the browser offers to try again")]
    public async Task Load_ServerOff_Offline()
    {
        // arrange
        var controller = new StartController(new FakeRecamApi { Offline = true });

        // act
        await controller.LoadAsync(CancellationToken.None);

        // assert
        Assert.Equal(StartState.Offline, controller.State);
        Assert.False(controller.Busy);
    }
}
