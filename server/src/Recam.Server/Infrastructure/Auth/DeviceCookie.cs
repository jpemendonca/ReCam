namespace Recam.Server.Infrastructure.Auth;

/// <summary>
/// Where a browser Monitor keeps its device credential (SPECS.md 5.4): HttpOnly, so no script
/// reads it, and SameSite=Strict, so other sites cannot make the browser send it.
/// </summary>
public static class DeviceCookie
{
    public const string Name = "recam_device";

    /// <summary>Header a browser must add when the cookie authenticates a request that changes something.</summary>
    public const string WebHeader = "X-Recam-Web";

    /// <summary>How long "Remember on this computer" lasts.</summary>
    public static readonly TimeSpan RememberFor = TimeSpan.FromDays(180);

    public static void Append(HttpResponse response, string credential, bool remember, DateTimeOffset now) =>
        response.Cookies.Append(Name, credential, Options(remember ? now + RememberFor : null));

    public static void Delete(HttpResponse response) => response.Cookies.Delete(Name, Options(null));

    private static CookieOptions Options(DateTimeOffset? expires) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = "/",
        IsEssential = true,
        Expires = expires,
    };
}
