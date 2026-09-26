using Microsoft.EntityFrameworkCore;
using Recam.Server.Domain;
using Recam.Server.Infrastructure.Hosting;
using Recam.Server.Infrastructure.Network;
using Recam.Server.Infrastructure.Persistence;

namespace Recam.Server.Features.Setup;

/// <summary>
/// Keeps one first-time code while the server has no active Monitor, prints it in the log, and
/// lets the browser that types it become the owner (SPECS.md 6). The code lives only in memory.
/// </summary>
public sealed partial class FirstOpen(
    IDbContextFactory<RecamDbContext> databaseFactory,
    TimeProvider timeProvider,
    ServerSettings settings,
    ILogger<FirstOpen> logger) : IDisposable
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private FirstOpenCode? _code;

    /// <summary>True while no Monitor is active, so a browser may still become the first one.</summary>
    public async Task<bool> IsOpenAsync(CancellationToken cancellationToken)
    {
        await using var database = await databaseFactory.CreateDbContextAsync(cancellationToken);
        return !await HasMonitorAsync(database, cancellationToken);
    }

    /// <summary>Issues and logs a code when the server has no Monitor, and forgets it when it has one.</summary>
    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            await using var database = await databaseFactory.CreateDbContextAsync(cancellationToken);
            if (await HasMonitorAsync(database, cancellationToken))
            {
                _code = null;
            }
            else if (_code is null)
            {
                _code = IssueCode();
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>Makes the browser the owner when the code is right. A spent code is replaced.</summary>
    public async Task<Result<PairedDevice>> OpenAsync(string? code, string browserName, CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            await using var database = await databaseFactory.CreateDbContextAsync(cancellationToken);
            var hasMonitor = await HasMonitorAsync(database, cancellationToken);
            _code ??= hasMonitor ? null : IssueCode();
            if (_code is null)
            {
                return SetupErrors.AlreadyHasMonitor;
            }

            var opened = _code.Open(code, browserName, hasMonitor, timeProvider.GetUtcNow());
            if (opened.IsFailure)
            {
                if (_code.IsSpent)
                {
                    _code = IssueCode();
                }

                return opened.Error;
            }

            database.Devices.Add(opened.Value.Device);
            await database.SaveChangesAsync(cancellationToken);
            _code = null;
            LogOpened(logger, opened.Value.Device.Id, opened.Value.Device.Name);
            return opened.Value;
        }
        finally
        {
            _lock.Release();
        }
    }

    public void Dispose() => _lock.Dispose();

    private static Task<bool> HasMonitorAsync(RecamDbContext database, CancellationToken cancellationToken) =>
        database.Devices.AnyAsync(
            device => (device.Role == DeviceRole.Owner || device.Role == DeviceRole.Viewer) && device.RevokedAt == null,
            cancellationToken);

    private FirstOpenCode IssueCode()
    {
        var code = FirstOpenCode.Issue();
        var urls = PublicUrlResolver.Resolve(settings, PublicUrlResolver.DetectLocalAddresses())
            .Select(url => url.GetLeftPart(UriPartial.Authority));
        LogCode(logger, string.Join(", ", urls), code.Display);
        return code;
    }

    // The only log line allowed to carry a secret (AGENTS.md): the operator reads it to claim the
    // server. It is not a credential and only works from the local network.
    [LoggerMessage(Level = LogLevel.Warning, Message =
        "No Monitor yet. On a computer in this network, open {ServerUrls} in the browser, accept the " +
        "certificate warning and type the first-time code {Code}")]
    private static partial void LogCode(ILogger logger, string serverUrls, string code);

    [LoggerMessage(Level = LogLevel.Information, Message = "Browser {DeviceId} ({Name}) is now the Monitor")]
    private static partial void LogOpened(ILogger logger, Guid deviceId, string name);
}
