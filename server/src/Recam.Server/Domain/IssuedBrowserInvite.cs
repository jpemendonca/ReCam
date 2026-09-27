namespace Recam.Server.Domain;

/// <param name="Claim">Goes in the invitation link; whoever opens it collects the credential.</param>
public sealed record IssuedBrowserInvite(BrowserLink Link, string Claim);
