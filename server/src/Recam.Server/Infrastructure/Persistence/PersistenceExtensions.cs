using Microsoft.EntityFrameworkCore;
using Recam.Server.Infrastructure.Hosting;

namespace Recam.Server.Infrastructure.Persistence;

public static class PersistenceExtensions
{
    public static IServiceCollection AddRecamPersistence(this IServiceCollection services, ServerSettings settings)
    {
        var databasePath = Path.Combine(settings.DataDirectory, "recam.db");
        services.AddDbContextFactory<RecamDbContext>(options => options.UseSqlite($"Data Source={databasePath}"));
        return services;
    }

    public static async Task ApplyMigrationsAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<RecamDbContext>();
        await database.Database.MigrateAsync();
    }
}
