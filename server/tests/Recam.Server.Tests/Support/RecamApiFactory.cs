using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Recam.Server.Infrastructure.Hosting;
using Recam.Server.Infrastructure.Persistence;

namespace Recam.Server.Tests.Support;

/// <summary>
/// Runs the whole API in memory with a throwaway data directory, a controllable clock and
/// requests that look like they come from the local network.
/// </summary>
public sealed class RecamApiFactory : WebApplicationFactory<Program>
{
    private readonly TemporaryDirectory _dataDirectory = new();

    public string DataDirectory => _dataDirectory.Path;

    public FakeTimeProvider Time { get; } = new(new DateTimeOffset(2026, 9, 24, 12, 0, 0, TimeSpan.Zero));

    public IPAddress RemoteIpAddress { get; set; } = IPAddress.Loopback;

    public async Task<RecamDbContext> CreateDatabaseAsync()
    {
        var factory = Services.GetRequiredService<IDbContextFactory<RecamDbContext>>();
        return await factory.CreateDbContextAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting(ServerSettings.DataDirectoryKey, _dataDirectory.Path);
        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<TimeProvider>(Time);
            services.AddSingleton<IStartupFilter>(new RemoteIpStartupFilter(() => RemoteIpAddress));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            // Pooled SQLite connections keep the file open, and Windows refuses to delete it.
            SqliteConnection.ClearAllPools();
            _dataDirectory.Dispose();
        }
    }

    private sealed class RemoteIpStartupFilter(Func<IPAddress> remoteIpAddress) : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use((context, nextMiddleware) =>
            {
                context.Connection.RemoteIpAddress = remoteIpAddress();
                return nextMiddleware(context);
            });
            next(app);
        };
    }
}
