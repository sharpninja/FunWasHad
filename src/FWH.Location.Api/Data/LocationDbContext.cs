using Microsoft.EntityFrameworkCore;

namespace FWH.Location.Api.Data;

public class LocationDbContext : DbContext
{
    public LocationDbContext(DbContextOptions<LocationDbContext> options) : base(options)
    {
    }

    public DbSet<LocationConfirmation> LocationConfirmations => Set<LocationConfirmation>();

    public DbSet<DeviceLocation> DeviceLocations => Set<DeviceLocation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LocationConfirmation>(entity =>
        {
            entity.ToTable("location_confirmations");
            entity.Property(e => e.BusinessName).IsRequired();
            entity.Property(e => e.BusinessLatitude).IsRequired();
            entity.Property(e => e.BusinessLongitude).IsRequired();
            entity.Property(e => e.UserLatitude).IsRequired();
            entity.Property(e => e.UserLongitude).IsRequired();
            entity.Property(e => e.ConfirmedAt).HasDefaultValueSql("now() at time zone 'utc'");
        });

        modelBuilder.Entity<DeviceLocation>(entity =>
        {
            entity.ToTable("device_locations");
            
            // Manually map PascalCase properties to snake_case columns to match PostgreSQL naming convention
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DeviceId).IsRequired().HasColumnName("device_id");
            entity.Property(e => e.Latitude).IsRequired().HasColumnName("latitude");
            entity.Property(e => e.Longitude).IsRequired().HasColumnName("longitude");
            entity.Property(e => e.AccuracyMeters).HasColumnName("accuracy_meters");
            entity.Property(e => e.Timestamp).IsRequired().HasColumnName("timestamp");
            entity.Property(e => e.RecordedAt).HasDefaultValueSql("now() at time zone 'utc'").HasColumnName("recorded_at");
            
            entity.HasIndex(e => e.DeviceId);
            entity.HasIndex(e => e.Timestamp);
        });
    }
}
