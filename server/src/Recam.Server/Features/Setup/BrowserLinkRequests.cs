namespace Recam.Server.Features.Setup;

public sealed record ApproveBrowserLinkRequest(string? Secret);

public sealed record ClaimBrowserLinkRequest(string? Claim, bool Remember);
