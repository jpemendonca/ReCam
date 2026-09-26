namespace Recam.Server.Domain;

public static class BrowserLinkErrors
{
    /// <summary>Unknown, expired, used and wrong-secret links all look the same, so nobody can probe them.</summary>
    public static readonly DomainError NotFound =
        new("browser_link.not_found", "This connection code is invalid or expired. Show a new one in the browser.", ErrorType.NotFound);

    public static readonly DomainError NotApproved =
        new("browser_link.not_approved", "No Monitor phone approved this browser yet.", ErrorType.Conflict);

    public static readonly DomainError ApproverNotMonitor =
        new("browser_link.approver_not_monitor", "Only a Monitor can connect a browser.", ErrorType.Forbidden);
}
