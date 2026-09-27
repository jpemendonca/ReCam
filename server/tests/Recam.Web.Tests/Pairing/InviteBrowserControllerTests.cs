using Recam.Web.Api;
using Recam.Web.Pairing;
using Recam.Web.Tests.Support;

namespace Recam.Web.Tests.Pairing;

public sealed class InviteBrowserControllerTests
{
    private readonly FakeRecamApi _api = new() { Me = new MeInfo(Guid.NewGuid(), "Navegador", "owner") };
    private readonly FakeClipboard _clipboard = new();

    [Fact(DisplayName = "The invitation shows its link as a QR code and says which address to keep using")]
    public async Task Start_ShowsLinkQrAndAddress()
    {
        // arrange
        var invite = new InviteBrowserController(_api, _clipboard);

        // act
        await invite.StartAsync();

        // assert
        Assert.Equal(InviteState.Ready, invite.State);
        Assert.Equal(_api.Invites.Single().Url, invite.Url);
        Assert.Contains("<svg", invite.QrSvg, StringComparison.Ordinal);
        Assert.Equal("https://cameras.example.com", invite.Address);
        Assert.Equal(TimeSpan.FromMinutes(10), invite.Remaining);
    }

    [Fact(DisplayName = "When the invitation expires unused, it says so, and a new one comes only when asked")]
    public async Task Tick_PastExpiry_SaysExpiredThenNewOnRequest()
    {
        // arrange
        var invite = new InviteBrowserController(_api, _clipboard);
        await invite.StartAsync();

        // act
        await invite.Tick(TimeSpan.FromMinutes(10));
        var expired = (invite.State, _api.Invites.Count);
        await invite.StartAsync();

        // assert
        Assert.Equal((InviteState.Expired, 1), expired);
        Assert.Equal(InviteState.Ready, invite.State);
        Assert.Equal(_api.Invites[1].Url, invite.Url);
    }

    [Fact(DisplayName = "Once a browser uses the link, the next check says so")]
    public async Task Tick_AfterUse_TurnsUsed()
    {
        // arrange
        var invite = new InviteBrowserController(_api, _clipboard);
        await invite.StartAsync();
        await invite.Tick(InviteBrowserController.CheckEvery);
        var before = invite.State;
        _api.UsedInvites.Add(_api.Invites[0].Id);

        // act
        await invite.Tick(InviteBrowserController.CheckEvery);

        // assert
        Assert.Equal(InviteState.Ready, before);
        Assert.Equal(InviteState.Used, invite.State);
    }

    [Fact(DisplayName = "Copy puts the link in the clipboard, and says so when the browser refuses")]
    public async Task Copy_CopiesOrSaysRefused()
    {
        // arrange
        var invite = new InviteBrowserController(_api, _clipboard);
        await invite.StartAsync();

        // act
        await invite.CopyAsync();
        var copied = invite.Copied;
        _clipboard.Refuses = true;
        await invite.CopyAsync();

        // assert
        Assert.True(copied);
        Assert.Equal([invite.Url!], _clipboard.Written);
        Assert.False(invite.Copied);
    }

    [Fact(DisplayName = "Without the server, the invitation says it failed")]
    public async Task Start_Offline_Failed()
    {
        // arrange
        _api.Offline = true;
        var invite = new InviteBrowserController(_api, _clipboard);

        // act
        await invite.StartAsync();

        // assert
        Assert.Equal(InviteState.Failed, invite.State);
    }
}
