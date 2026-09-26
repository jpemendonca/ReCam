namespace Recam.Server.Features.Setup;

/// <summary>
/// <see cref="QrUri"/> goes on screen for a Monitor phone; <see cref="Claim"/> stays in the
/// browser, which uses it to collect its credential once the phone approved.
/// </summary>
public sealed record BrowserLinkResponse(Guid Id, string QrUri, string Claim, DateTimeOffset ExpiresAt);
