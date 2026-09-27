namespace Recam.Web.Api;

/// <summary>An invitation link for another browser, with its claim in the fragment, and how long it lasts.</summary>
public sealed record BrowserInviteInfo(Guid Id, string Url, TimeSpan ValidFor);
