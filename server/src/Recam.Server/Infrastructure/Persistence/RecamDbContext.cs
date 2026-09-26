using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Recam.Server.Domain;

namespace Recam.Server.Infrastructure.Persistence;

public sealed class RecamDbContext(DbContextOptions<RecamDbContext> options) : DbContext(options)
{
    public DbSet<Device> Devices => Set<Device>();

    public DbSet<PairingToken> PairingTokens => Set<PairingToken>();

    public DbSet<BrowserLink> BrowserLinks => Set<BrowserLink>();

    public DbSet<RecordingQuota> RecordingQuotas => Set<RecordingQuota>();

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

            // Cameras paired before motion detection start at medium. The sentinel is a value no
            // camera has, so EF still writes "low" when someone picks it.
            device.Property(d => d.MotionSensitivity)
                .HasDefaultValue(MotionSensitivity.Medium)
                .HasSentinel((MotionSensitivity)(-1));
        });

        modelBuilder.Entity<PairingToken>(token =>
        {
            token.HasKey(t => t.Id);
            token.Property(t => t.TokenHash).HasMaxLength(32);
            token.HasIndex(t => t.TokenHash).IsUnique();

            // Two requests racing with the same token: only the first save wins, the other fails.
            token.Property(t => t.UsedAt).IsConcurrencyToken();
        });

        modelBuilder.Entity<BrowserLink>(link =>
        {
            link.HasKey(l => l.Id);
            link.Property(l => l.ClaimHash).HasMaxLength(32);
            link.Property(l => l.ApprovalHash).HasMaxLength(32);

            // A claim racing another claim of the same link: only the first save wins.
            link.Property(l => l.DeviceId).IsConcurrencyToken();
        });

        modelBuilder.Entity<RecordingQuota>(quota =>
        {
            quota.HasKey(q => q.Id);
            quota.Property(q => q.Id).ValueGeneratedNever();
        });
    }
}
