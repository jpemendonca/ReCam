using Recam.Web.Api;

namespace Recam.Web.Tests.Support;

/// <summary>A server in memory: one first-time code, and at most one Monitor.</summary>
public sealed class FakeRecamApi : IRecamApi
{
    public const string Code = "ABCD-EFGH";

    public MeInfo? Me { get; set; }

    /// <summary>A Monitor that is not this browser.</summary>
    public bool OtherMonitor { get; set; }

    public bool Offline { get; set; }

    public List<bool> RememberSent { get; } = [];

    public Task<MeInfo?> GetMeAsync(CancellationToken cancellationToken)
    {
        ThrowIfOffline();
        return Task.FromResult(Me);
    }

    public Task<bool> IsFirstOpenAsync(CancellationToken cancellationToken)
    {
        ThrowIfOffline();
        return Task.FromResult(Me is null && !OtherMonitor);
    }

    public Task<FirstOpenOutcome> FirstOpenAsync(string code, bool remember, CancellationToken cancellationToken)
    {
        ThrowIfOffline();
        RememberSent.Add(remember);
        if (Me is not null || OtherMonitor)
        {
            return Task.FromResult(FirstOpenOutcome.AlreadyHasMonitor);
        }

        if (!string.Equals(code, Code, StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(FirstOpenOutcome.WrongCode);
        }

        Me = new MeInfo(Guid.NewGuid(), "Navegador · Chrome no Windows", "owner");
        return Task.FromResult(FirstOpenOutcome.Opened);
    }

    public Task SignOutAsync(CancellationToken cancellationToken)
    {
        ThrowIfOffline();
        Me = null;
        return Task.CompletedTask;
    }

    private void ThrowIfOffline()
    {
        if (Offline)
        {
            throw new HttpRequestException("The server is off.");
        }
    }
}
