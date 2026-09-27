using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Recam.Web.Api;
using Recam.Web.Join;
using Recam.Web.Pages;
using Recam.Web.Tests.Support;

namespace Recam.Web.Tests.Join;

public sealed class JoinPageTests : BunitContext
{
    private readonly FakeRecamApi _api = new() { OtherMonitor = true };

    public JoinPageTests()
    {
        Services.AddLocalization();
        Services.AddSingleton<IRecamApi>(_api);
        Services.AddTransient<JoinController>();
    }

    [Fact(DisplayName = "Opening an invitation and accepting it makes this browser a Monitor and starts over")]
    public async Task Join_ValidInvite_BecomesMonitor()
    {
        // arrange
        using var _ = Culture.Use("pt-BR");
        var invite = await _api.CreateBrowserInviteAsync(CancellationToken.None);
        var page = RenderAt(invite.Url);
        page.WaitForAssertion(() => Assert.Equal("Assistir neste navegador", page.Find("button").TextContent));

        // act
        page.Find("input[type=checkbox]").Change(false);
        page.Find("button").Click();

        // assert
        page.WaitForAssertion(() => Assert.Equal("viewer", _api.Me?.Role));
        Assert.Equal([false], _api.RememberSent);
        var navigation = (BunitNavigationManager)Services.GetRequiredService<NavigationManager>();
        Assert.Equal("http://localhost/", navigation.Uri);
        Assert.DoesNotContain(invite.Url.Split('#')[1], navigation.Uri, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "A used invitation says to ask for a new one")]
    public async Task Join_UsedInvite_SaysGone()
    {
        // arrange
        using var _ = Culture.Use("en-US");
        var invite = await _api.CreateBrowserInviteAsync(CancellationToken.None);
        var link = InviteLink.Parse(invite.Url)!;
        await _api.ClaimBrowserLinkAsync(link.Id, link.Claim, remember: false, CancellationToken.None);
        _api.Me = null;
        var page = RenderAt(invite.Url);

        // act
        page.Find("button").Click();

        // assert
        page.WaitForAssertion(() => Assert.Equal(
            "This invitation expired or was already used. On the Monitor, open Add Monitor › In a browser again.",
            page.Find(".error").TextContent));
    }

    [Fact(DisplayName = "An address without a complete invitation says so")]
    public void Render_NoFragment_SaysIncomplete()
    {
        // arrange
        using var _ = Culture.Use("en-US");

        // act
        var page = RenderAt("http://localhost/connect#l=abc");

        // assert
        Assert.StartsWith("This invitation link is incomplete.", page.Find(".error").TextContent, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "A browser that is already a Monitor keeps the invitation unused")]
    public async Task Render_AlreadyMonitor_DoesNotClaim()
    {
        // arrange
        using var _ = Culture.Use("en-US");
        var invite = await _api.CreateBrowserInviteAsync(CancellationToken.None);
        _api.Me = new MeInfo(Guid.NewGuid(), "Navegador", "owner");

        // act
        var page = RenderAt(invite.Url);

        // assert
        page.WaitForAssertion(() => Assert.Equal("This browser is already a Monitor.", page.Find("p").TextContent));
        Assert.Empty(_api.RememberSent);
    }

    [Fact(DisplayName = "The invitation link is read from the fragment, and anything missing makes it invalid")]
    public void Parse_Fragment_ReadsOrRejects()
    {
        // arrange
        var id = Guid.NewGuid();

        // act
        var read = InviteLink.Parse($"https://cameras.example.com/connect#l={id:N}&c=abc-_123");
        var noClaim = InviteLink.Parse($"https://cameras.example.com/connect#l={id:N}");
        var noFragment = InviteLink.Parse("https://cameras.example.com/connect");

        // assert
        Assert.Equal(new InviteLink(id, "abc-_123"), read);
        Assert.Null(noClaim);
        Assert.Null(noFragment);
    }

    private IRenderedComponent<JoinPage> RenderAt(string url)
    {
        var fragment = url.Contains('#', StringComparison.Ordinal) ? url[url.IndexOf('#', StringComparison.Ordinal)..] : string.Empty;
        Services.GetRequiredService<NavigationManager>().NavigateTo("connect" + fragment);
        return Render<JoinPage>();
    }
}
