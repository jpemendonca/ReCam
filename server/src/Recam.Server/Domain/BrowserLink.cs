using System.Security.Cryptography;

namespace Recam.Server.Domain;

/// <summary>
/// "Connect browser" (SPECS.md 2.5): a browser that is not a Monitor shows a QR code; a Monitor
/// phone reads it and approves; the browser then collects its own credential and becomes a
/// Viewer. Two secrets, so that seeing the QR code is not enough to take the credential.
/// </summary>
public sealed class BrowserLink
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(10);

    private BrowserLink()
    {
        ClaimHash = [];
        ApprovalHash = [];
    }

    public Guid Id { get; private set; }

    public byte[] ClaimHash { get; private set; }

    public byte[] ApprovalHash { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public Guid? ApprovedBy { get; private set; }

    /// <summary>The device created for the browser once it collected its credential.</summary>
    public Guid? DeviceId { get; private set; }

    public static IssuedBrowserLink Issue(DateTimeOffset now)
    {
        var claim = SecretToken.Generate();
        var approval = SecretToken.Generate();
        var link = new BrowserLink
        {
            Id = Guid.CreateVersion7(now),
            ClaimHash = SecretToken.Hash(claim),
            ApprovalHash = SecretToken.Hash(approval),
            CreatedAt = now,
            ExpiresAt = now.Add(Lifetime),
        };
        return new IssuedBrowserLink(link, claim, approval);
    }

    public Result Approve(string? approval, Device approver, DateTimeOffset now)
    {
        if (!IsOpen(now) || ApprovedBy is not null || !Matches(approval, ApprovalHash))
        {
            return BrowserLinkErrors.NotFound;
        }

        if (!approver.IsMonitor || approver.IsRevoked)
        {
            return BrowserLinkErrors.ApproverNotMonitor;
        }

        ApprovedBy = approver.Id;
        return Result.Success();
    }

    /// <summary>Hands the browser its credential, once, after a Monitor approved.</summary>
    public Result<PairedDevice> Claim(string? claim, string browserName, DateTimeOffset now)
    {
        if (!IsOpen(now) || DeviceId is not null || !Matches(claim, ClaimHash))
        {
            return BrowserLinkErrors.NotFound;
        }

        if (ApprovedBy is null)
        {
            return BrowserLinkErrors.NotApproved;
        }

        var paired = Device.CreateLinkedBrowser(browserName, now);
        DeviceId = paired.Device.Id;
        return paired;
    }

    public bool IsExpired(DateTimeOffset now) => now >= ExpiresAt;

    private bool IsOpen(DateTimeOffset now) => !IsExpired(now);

    private static bool Matches(string? secret, byte[] hash) =>
        secret is { Length: > 0 } && CryptographicOperations.FixedTimeEquals(SecretToken.Hash(secret), hash);
}
