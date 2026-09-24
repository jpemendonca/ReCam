using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Recam.Server.Domain;

namespace Recam.Server.Infrastructure.Persistence;

public sealed class RecamDbContext(DbContextOptions<RecamDbContext> options) : DbContext(options)
{
    public DbSet<Device> Devices => Set<Device>();

    public DbSet<PairingToken> PairingTokens => Set<PairingToken>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // SQLite has no DateTimeOffset type; stored as binary so WHERE and ORDER BY still translate.
        configurationBuilder.Properties<DateTimeOffset>().HaveConversion<DateTimeOffsetToBinaryConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Device>(device =>
        {
            device.HasKey(d => d.Id);
            device.Property(d => d.Name).HasMaxLength(40);
            device.Property(d => d.CredentialHash).HasMaxLength(32);
            device.HasIndex(d => d.Role);
        });

        modelBuilder.Entity<PairingToken>(token =>
        {
            token.HasKey(t => t.Id);
            token.Property(t => t.TokenHash).HasMaxLength(32);
            token.HasIndex(t => t.TokenHash).IsUnique();

            // Two requests racing with the same token: only the first save wins, the other fails.
            token.Property(t => t.UsedAt).IsConcurrencyToken();
        });
    }
}
