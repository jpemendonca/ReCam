using Microsoft.EntityFrameworkCore;
using Recam.Server.Domain;
using Recam.Server.Infrastructure.Hosting;
using Recam.Server.Infrastructure.Network;
using Recam.Server.Infrastructure.Persistence;

namespace Recam.Server.Features.Setup;

/// <summary>
/// <c>docker compose exec server ./Recam.Server code</c>: shows again the address and the
/// first-time code the server printed when it started, without searching the log.
/// </summary>
public static class FirstOpenCodeCommand
{
    public const string Argument = "code";

    public static async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        var builder = WebApplication.CreateBuilder();
        var settings = ServerSettings.From(builder.Configuration, builder.Environment.ContentRootPath);
        await using var database = PersistenceExtensions.CreateDbContext(settings);
        await database.Database.MigrateAsync(cancellationToken);
        var urls = PublicUrlResolver.Resolve(settings, PublicUrlResolver.DetectLocalAddresses());
        await Console.Out.WriteLineAsync(await DescribeAsync(database, settings, urls, cancellationToken));
        return 0;
    }

    /// <summary>What to tell the person: where to go and which code to type, or why there is none.</summary>
    public static async Task<string> DescribeAsync(
        RecamDbContext database, ServerSettings settings, IEnumerable<Uri> serverUrls, CancellationToken cancellationToken)
    {
        var hasMonitor = await database.Devices.AnyAsync(
            device => (device.Role == DeviceRole.Owner || device.Role == DeviceRole.Viewer) && device.RevokedAt == null,
            cancellationToken);
        if (hasMonitor)
        {
            return "This server already has a Monitor, so there is no first-time code. Add browsers and phones from it, " +
                "or start over with: docker compose exec server ./Recam.Server reset-owner";
        }

        var code = await FirstOpenCodeFile.ReadAsync(settings.DataDirectory, cancellationToken);
        if (code is null)
        {
            return "The server has not made a first-time code yet. Wait a few seconds and run this again.";
        }

        return Banner(string.Join(", ", serverUrls.Select(url => url.GetLeftPart(UriPartial.Authority))), code);
    }

    /// <summary>The same steps the server prints in its log.</summary>
    public static string Banner(string serverUrls, string code) => $"""
        ==================== ReCam ====================
        No Monitor yet. On a computer in this network:
          1. Open {serverUrls} in the browser and accept the certificate warning.
          2. Type the first-time code {code}
        To see this again: docker compose exec server ./Recam.Server code
        ===============================================
        """;
}
