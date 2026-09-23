using AstralObservatory.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AstralObservatory.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<CelestialObject> CelestialObjects => Set<CelestialObject>();
    public DbSet<Observation> Observations => Set<Observation>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<PushSubscription> PushSubscriptions => Set<PushSubscription>();
    public DbSet<ObservationAlert> ObservationAlerts => Set<ObservationAlert>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>()
            .Property(user => user.Name)
            .HasMaxLength(100);

        builder.Entity<ApplicationUser>()
            .Property(user => user.PreferredTwoFactorMethod)
            .HasConversion<string>();

        builder.Entity<CelestialObject>()
            .Property(item => item.Type)
            .HasConversion<string>();

        builder.Entity<CelestialObject>()
            .Property(item => item.Name)
            .HasMaxLength(100);

        builder.Entity<Observation>()
            .HasIndex(item => new { item.CelestialObjectId, item.ObservedAt });

        builder.Entity<Notification>()
            .HasIndex(item => new { item.UserId, item.IsRead, item.CreatedAt });

        builder.Entity<PushSubscription>()
            .HasIndex(item => item.Endpoint)
            .IsUnique();

        builder.Entity<ObservationAlert>()
            .HasIndex(item => new { item.UserId, item.IsActive });

        builder.Entity<SystemSetting>()
            .HasKey(item => item.Key);
    }
}
