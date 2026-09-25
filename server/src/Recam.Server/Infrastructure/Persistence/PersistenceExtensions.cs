using Microsoft.EntityFrameworkCore;
using Recam.Server.Infrastructure.Hosting;

namespace Recam.Server.Infrastructure.Persistence;

public static class PersistenceExtensions
{
    public static IServiceCollection AddRecamPersistence(this IServiceCollection services, ServerSettings settings)
    {
        services.AddDbContextFactory<RecamDbContext>(options => options.UseSqlite(ConnectionString(settings)));
        return services;
    }

    /// <summary>A context outside the web host, for command-line tools such as reset-owner.</summary>
    public static RecamDbContext CreateDbContext(ServerSettings settings) =>
        new(new DbContextOptionsBuilder<RecamDbContext>().UseSqlite(ConnectionString(settings)).Options);

    private static string ConnectionString(ServerSettings settings) =>
        $"Data Source={Path.Combine(settings.DataDirectory, "recam.db")}";

    public static async Task ApplyMigrationsAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<RecamDbContext>();
        await database.Database.MigrateAsync();
    }
}
