using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Recam.Server.Domain;
using Recam.Server.Features.Pairing;
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

    /// <summary>Stores a fresh pairing token and returns its secret, as a QR code would carry it.</summary>
    public async Task<string> CreatePairingTokenAsync(DeviceRole role)
    {
        var issued = PairingToken.Issue(role, Time.GetUtcNow());
        await using var database = await CreateDatabaseAsync();
        database.PairingTokens.Add(issued.Token);
        await database.SaveChangesAsync();
        return issued.Secret;
    }

    /// <summary>Pairs a new device through the API and returns its credential.</summary>
    public async Task<PairResponse> PairDeviceAsync(DeviceRole role, string name = "Test device")
    {
        var token = await CreatePairingTokenAsync(role);
        using var client = CreateClient();
        using var response = await client.PairAsync(token, name);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PairResponse>(ApiJson.Options))!;
    }

    public HttpClient CreateDeviceClient(string credential)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", credential);
        return client;
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
