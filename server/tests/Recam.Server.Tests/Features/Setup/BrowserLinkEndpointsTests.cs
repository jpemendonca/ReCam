using System.Net;
using System.Net.Http.Json;
using System.Web;
using Recam.Server.Domain;
using Recam.Server.Features.Devices;
using Recam.Server.Features.Setup;
using Recam.Server.Tests.Support;

namespace Recam.Server.Tests.Features.Setup;

public sealed class BrowserLinkEndpointsTests
{
    private static readonly Uri LinksUri = new("/api/browser-links", UriKind.Relative);

    private static readonly Uri InviteUri = new("/api/browser-links/invite", UriKind.Relative);

    [Fact(DisplayName = "A Monitor phone approves the QR, and the browser becomes a Monitor with its cookie")]
    public async Task CreateApproveClaim_BrowserBecomesViewer()
    {
        // arrange
        using var factory = new RecamApiFactory();
        var owner = await factory.PairDeviceAsync(DeviceRole.Owner);
        using var browser = factory.CreateBrowserClient();
        var link = await CreateAsync(browser);
        using var phone = factory.CreateDeviceClient(owner.Credential);
        using var approved = await phone.PostAsJsonAsync(ApproveUri(link), new { secret = SecretOf(link) }, ApiJson.Options, TestContext.Current.CancellationToken);

        // act
        using var claimed = await ClaimAsync(browser, link, link.Claim);

        // assert
        Assert.Equal(HttpStatusCode.NoContent, approved.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, claimed.StatusCode);
        var cookie = Assert.Single(claimed.Headers.GetValues("Set-Cookie")).Split(';')[0];
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("/api/me", UriKind.Relative));
        request.Headers.Add("Cookie", cookie);
        using var me = await browser.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await me.Content.ReadFromJsonAsync<MeResponse>(ApiJson.Options, TestContext.Current.CancellationToken);
        Assert.Equal(DeviceRole.Viewer, body!.Role);
        Assert.StartsWith("recam://connect-browser?v=1&l=", link.QrUri, StringComparison.Ordinal);
        Assert.DoesNotContain(link.Claim, link.QrUri, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Before the phone approves, the browser keeps waiting")]
    public async Task Claim_BeforeApproval_Conflict()
    {
        // arrange
        using var factory = new RecamApiFactory();
        using var browser = factory.CreateBrowserClient();
        var link = await CreateAsync(browser);

        // act
        using var claimed = await ClaimAsync(browser, link, link.Claim);

        // assert
        Assert.Equal(HttpStatusCode.Conflict, claimed.StatusCode);
    }

    [Fact(DisplayName = "Someone who only saw the QR code cannot collect the credential")]
    public async Task Claim_WithQrSecret_NotFound()
    {
        // arrange
        using var factory = new RecamApiFactory();
        var owner = await factory.PairDeviceAsync(DeviceRole.Owner);
        using var browser = factory.CreateBrowserClient();
        var link = await CreateAsync(browser);
        using var phone = factory.CreateDeviceClient(owner.Credential);
        using var _ = await phone.PostAsJsonAsync(ApproveUri(link), new { secret = SecretOf(link) }, ApiJson.Options, TestContext.Current.CancellationToken);

        // act
        using var claimed = await ClaimAsync(browser, link, SecretOf(link));

        // assert
        Assert.Equal(HttpStatusCode.NotFound, claimed.StatusCode);
    }

    [Fact(DisplayName = "A camera cannot approve a browser")]
    public async Task Approve_ByCamera_Forbidden()
    {
        // arrange
        using var factory = new RecamApiFactory();
        var camera = await factory.PairDeviceAsync(DeviceRole.Camera);
        using var browser = factory.CreateBrowserClient();
        var link = await CreateAsync(browser);
        using var phone = factory.CreateDeviceClient(camera.Credential);

        // act
        using var approved = await phone.PostAsJsonAsync(ApproveUri(link), new { secret = SecretOf(link) }, ApiJson.Options, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.Forbidden, approved.StatusCode);
    }

    [Fact(DisplayName = "A wrong QR secret and an unknown link look the same")]
    public async Task Approve_WrongSecretOrUnknown_NotFound()
    {
        // arrange
        using var factory = new RecamApiFactory();
        var owner = await factory.PairDeviceAsync(DeviceRole.Owner);
        using var browser = factory.CreateBrowserClient();
        var link = await CreateAsync(browser);
        using var phone = factory.CreateDeviceClient(owner.Credential);

        // act
        using var wrong = await phone.PostAsJsonAsync(ApproveUri(link), new { secret = "wrong" }, ApiJson.Options, TestContext.Current.CancellationToken);
        using var unknown = await phone.PostAsJsonAsync(
            new Uri($"/api/browser-links/{Guid.NewGuid()}/approve", UriKind.Relative), new { secret = SecretOf(link) }, ApiJson.Options, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.NotFound, wrong.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
    }

    [Fact(DisplayName = "A browser Monitor invites another browser, which opens the link and becomes a Monitor")]
    public async Task Invite_ByBrowserMonitor_LinkMakesViewer()
    {
        // arrange
        using var factory = new RecamApiFactory();
        var monitorCookie = await factory.OpenBrowserMonitorAsync();
        using var browser = factory.CreateBrowserClient();
        var invite = await InviteAsync(browser, monitorCookie);
        var (id, claim) = PartsOf(invite);

        // act
        using var claimed = await ClaimAsync(browser, id, claim);

        // assert
        Assert.Equal(HttpStatusCode.NoContent, claimed.StatusCode);
        Assert.StartsWith("https://localhost/connect#l=", invite.Url, StringComparison.Ordinal);
        Assert.Equal(factory.Time.GetUtcNow().Add(BrowserLink.Lifetime), invite.ExpiresAt);
        var cookie = Assert.Single(claimed.Headers.GetValues("Set-Cookie")).Split(';')[0];
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("/api/me", UriKind.Relative));
        request.Headers.Add("Cookie", cookie);
        using var me = await browser.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await me.Content.ReadFromJsonAsync<MeResponse>(ApiJson.Options, TestContext.Current.CancellationToken);
        Assert.Equal(DeviceRole.Viewer, body!.Role);
        Assert.DoesNotContain(factory.Logs.Messages, message => message.Contains(claim, StringComparison.Ordinal));
    }

    [Fact(DisplayName = "With a public URL, the invitation link uses it instead of the address in use")]
    public async Task Invite_WithPublicUrl_UsesIt()
    {
        // arrange
        using var factory = new RecamApiFactory { Settings = new Dictionary<string, string> { ["RECAM_PUBLIC_URLS"] = "https://cameras.example.com" } };
        var monitorCookie = await factory.OpenBrowserMonitorAsync();
        using var browser = factory.CreateBrowserClient();

        // act
        var invite = await InviteAsync(browser, monitorCookie);

        // assert
        Assert.StartsWith("https://cameras.example.com/connect#l=", invite.Url, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "An invitation works once, and not after ten minutes")]
    public async Task Claim_InviteUsedOrExpired_NotFound()
    {
        // arrange
        using var factory = new RecamApiFactory();
        var monitorCookie = await factory.OpenBrowserMonitorAsync();
        using var browser = factory.CreateBrowserClient();
        var (usedId, usedClaim) = PartsOf(await InviteAsync(browser, monitorCookie));
        var (lateId, lateClaim) = PartsOf(await InviteAsync(browser, monitorCookie));
        using var first = await ClaimAsync(browser, usedId, usedClaim);

        // act
        using var again = await ClaimAsync(browser, usedId, usedClaim);
        factory.Time.Advance(BrowserLink.Lifetime);
        using var late = await ClaimAsync(browser, lateId, lateClaim);

        // assert
        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, again.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, late.StatusCode);
    }

    [Fact(DisplayName = "Only a Monitor can invite a browser")]
    public async Task Invite_ByCameraOrStranger_Refused()
    {
        // arrange
        using var factory = new RecamApiFactory();
        var camera = await factory.PairDeviceAsync(DeviceRole.Camera);
        using var phone = factory.CreateDeviceClient(camera.Credential);
        using var stranger = factory.CreateBrowserClient();

        // act
        using var byCamera = await phone.PostAsync(InviteUri, null, TestContext.Current.CancellationToken);
        using var byStranger = await stranger.PostAsync(InviteUri, null, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.Forbidden, byCamera.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, byStranger.StatusCode);
    }

    [Fact(DisplayName = "A Monitor phone can also invite a browser")]
    public async Task Invite_ByMonitorPhone_Created()
    {
        // arrange
        using var factory = new RecamApiFactory();
        var owner = await factory.PairDeviceAsync(DeviceRole.Owner);
        using var phone = factory.CreateDeviceClient(owner.Credential);

        // act
        using var response = await phone.PostAsync(InviteUri, null, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private static async Task<BrowserInviteResponse> InviteAsync(HttpClient browser, string monitorCookie)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, InviteUri);
        request.Headers.Add("Cookie", monitorCookie);
        request.Headers.Add("X-Recam-Web", "1");
        using var response = await browser.SendAsync(request, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<BrowserInviteResponse>(ApiJson.Options, TestContext.Current.CancellationToken))!;
    }

    private static (Guid Id, string Claim) PartsOf(BrowserInviteResponse invite)
    {
        var fragment = HttpUtility.ParseQueryString(new Uri(invite.Url).Fragment.TrimStart('#'));
        return (Guid.Parse(fragment["l"]!), fragment["c"]!);
    }

    private static Task<HttpResponseMessage> ClaimAsync(HttpClient browser, Guid id, string claim) =>
        browser.PostAsJsonAsync(
            new Uri($"/api/browser-links/{id}/claim", UriKind.Relative), new { claim, remember = false }, ApiJson.Options, TestContext.Current.CancellationToken);

    private static async Task<BrowserLinkResponse> CreateAsync(HttpClient browser)
    {
        using var response = await browser.PostAsync(LinksUri, null, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<BrowserLinkResponse>(ApiJson.Options, TestContext.Current.CancellationToken))!;
    }

    private static Task<HttpResponseMessage> ClaimAsync(HttpClient browser, BrowserLinkResponse link, string claim) =>
        browser.PostAsJsonAsync(
            new Uri($"/api/browser-links/{link.Id}/claim", UriKind.Relative), new { claim, remember = false }, ApiJson.Options, TestContext.Current.CancellationToken);

    private static Uri ApproveUri(BrowserLinkResponse link) => new($"/api/browser-links/{link.Id}/approve", UriKind.Relative);

    private static string SecretOf(BrowserLinkResponse link) => HttpUtility.ParseQueryString(new Uri(link.QrUri).Query)["s"]!;
}
