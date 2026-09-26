namespace Recam.Web.Api;

/// <summary>The server's REST API, as the browser Monitor uses it. Faked in tests.</summary>
public interface IRecamApi
{
    /// <summary>This browser's device, or null when the browser is not a Monitor (no cookie, or revoked).</summary>
    Task<MeInfo?> GetMeAsync(CancellationToken cancellationToken);

    /// <summary>True while the server has no Monitor, so this browser may become the first one.</summary>
    Task<bool> IsFirstOpenAsync(CancellationToken cancellationToken);

    Task<FirstOpenOutcome> FirstOpenAsync(string code, bool remember, CancellationToken cancellationToken);

    Task SignOutAsync(CancellationToken cancellationToken);
}
