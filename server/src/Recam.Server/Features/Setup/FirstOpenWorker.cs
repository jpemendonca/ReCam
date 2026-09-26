namespace Recam.Server.Features.Setup;

/// <summary>
/// Checks every 30 seconds whether the server has a Monitor, so a new code shows up in the log
/// soon after the last Monitor leaves (or after reset-owner).
/// </summary>
public sealed class FirstOpenWorker(FirstOpen firstOpen, TimeProvider timeProvider) : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(CheckInterval, timeProvider);
        do
        {
            await firstOpen.RefreshAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
