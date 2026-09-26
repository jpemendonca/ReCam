namespace Recam.Server.Domain;

/// <param name="Claim">Stays in the browser that asked; it collects the credential.</param>
/// <param name="Approval">Goes in the QR code that a Monitor phone reads.</param>
public sealed record IssuedBrowserLink(BrowserLink Link, string Claim, string Approval);
