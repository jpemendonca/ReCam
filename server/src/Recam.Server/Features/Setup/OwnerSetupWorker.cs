namespace Recam.Server.Features.Setup;

/// <summary>Renews the owner token before it expires, so the QR in the log is always valid.</summary>
public sealed class OwnerSetupWorker(OwnerSetup ownerSetup, TimeProvider timeProvider) : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(CheckInterval, timeProvider);
        do
        {
            await ownerSetup.EnsureTokenAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
