using Recam.Server.Domain;

namespace Recam.Server.Tests.Domain;

public sealed class BrowserLinkTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 26, 12, 0, 0, TimeSpan.Zero);

    private static Device Monitor() => Device.CreateFirstMonitor("Navegador", Now).Device;

    [Fact(DisplayName = "After a Monitor approves, the browser collects a viewer credential once")]
    public void ApproveThenClaim_CreatesViewerOnce()
    {
        // arrange
        var issued = BrowserLink.Issue(Now);
        var approved = issued.Link.Approve(issued.Approval, Monitor(), Now);

        // act
        var claimed = issued.Link.Claim(issued.Claim, "Navegador · Firefox no Linux", Now);
        var again = issued.Link.Claim(issued.Claim, "Navegador · Firefox no Linux", Now);

        // assert
        Assert.True(approved.IsSuccess);
        Assert.Equal(DeviceRole.Viewer, claimed.Value.Device.Role);
        Assert.Equal("Navegador · Firefox no Linux", claimed.Value.Device.Name);
        Assert.Equal(claimed.Value.Device.Id, issued.Link.DeviceId);
        Assert.Equal(BrowserLinkErrors.NotFound, again.Error);
    }

    [Fact(DisplayName = "Before a Monitor approves, the browser waits")]
    public void Claim_NotApproved_Waits()
    {
        // arrange
        var issued = BrowserLink.Issue(Now);

        // act
        var claimed = issued.Link.Claim(issued.Claim, "Navegador", Now);

        // assert
        Assert.Equal(BrowserLinkErrors.NotApproved, claimed.Error);
    }

    [Fact(DisplayName = "The QR secret cannot collect the credential, and the claim cannot approve")]
    public void Secrets_AreNotInterchangeable()
    {
        // arrange
        var issued = BrowserLink.Issue(Now);

        // act
        var approvedWithClaim = issued.Link.Approve(issued.Claim, Monitor(), Now);
        issued.Link.Approve(issued.Approval, Monitor(), Now);
        var claimedWithApproval = issued.Link.Claim(issued.Approval, "Navegador", Now);

        // assert
        Assert.Equal(BrowserLinkErrors.NotFound, approvedWithClaim.Error);
        Assert.Equal(BrowserLinkErrors.NotFound, claimedWithApproval.Error);
    }

    [Fact(DisplayName = "A camera cannot let a browser in")]
    public void Approve_ByCamera_Forbidden()
    {
        // arrange
        var issued = BrowserLink.Issue(Now);
        var token = PairingToken.Issue(DeviceRole.Camera, Now);
        var camera = Device.Pair(token.Token, "Porta", [DeviceRole.Camera], Now).Value.Device;

        // act
        var approved = issued.Link.Approve(issued.Approval, camera, Now);

        // assert
        Assert.Equal(BrowserLinkErrors.ApproverNotMonitor, approved.Error);
    }

    [Fact(DisplayName = "After ten minutes the link is gone")]
    public void Approve_Expired_NotFound()
    {
        // arrange
        var issued = BrowserLink.Issue(Now);

        // act
        var approved = issued.Link.Approve(issued.Approval, Monitor(), Now + BrowserLink.Lifetime);

        // assert
        Assert.Equal(BrowserLinkErrors.NotFound, approved.Error);
    }
}
