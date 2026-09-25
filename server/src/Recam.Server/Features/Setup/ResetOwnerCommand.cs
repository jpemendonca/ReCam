using Microsoft.EntityFrameworkCore;
using Recam.Server.Domain;
using Recam.Server.Infrastructure.Hosting;
using Recam.Server.Infrastructure.Persistence;

namespace Recam.Server.Features.Setup;

/// <summary>
/// <c>docker compose exec server ./Recam.Server reset-owner</c>: the way out when the only Monitor
/// broke or disappeared. Takes every Monitor (owner and viewers) off the server; cameras stay
/// paired. The running server then offers the first-Monitor QR again, in its log and at /setup.
/// </summary>
public static class ResetOwnerCommand
{
    public const string Argument = "reset-owner";

    public static async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        var builder = WebApplication.CreateBuilder();
        var settings = ServerSettings.From(builder.Configuration, builder.Environment.ContentRootPath);
        await using var database = PersistenceExtensions.CreateDbContext(settings);
        await database.Database.MigrateAsync(cancellationToken);
        var removed = await RevokeMonitorsAsync(database, TimeProvider.System.GetUtcNow(), cancellationToken);
        await Console.Out.WriteLineAsync(
            $"Removed {removed} Monitor(s). Within 30 seconds the server shows a new QR code for the first Monitor " +
            $"in its log (docker compose logs server) and at https://<server-ip>:{ServerSettings.HttpsPort}/setup.");
        return 0;
    }

    /// <summary>Revokes every active Monitor and returns how many there were.</summary>
    public static async Task<int> RevokeMonitorsAsync(RecamDbContext database, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var monitors = await database.Devices
            .Where(device => (device.Role == DeviceRole.Owner || device.Role == DeviceRole.Viewer) && device.RevokedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var monitor in monitors)
        {
            monitor.Revoke(now);
        }

        await database.SaveChangesAsync(cancellationToken);
        return monitors.Count;
    }
}
