namespace Recam.Server.Features.Setup;

/// <summary>
/// "Add Monitor › In a browser": <see cref="Url"/> opens the web Monitor with the claim in the
/// fragment, which browsers never send to a server, so it stays out of server and proxy logs.
/// </summary>
public sealed record BrowserInviteResponse(Guid Id, string Url, DateTimeOffset ExpiresAt);
