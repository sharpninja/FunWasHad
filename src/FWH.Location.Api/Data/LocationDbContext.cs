using Microsoft.EntityFrameworkCore;

namespace FWH.Location.Api.Data;

public class LocationDbContext : DbContext
{
    public LocationDbContext(DbContextOptions<LocationDbContext> options) : base(options)
    {
    }

    public DbSet<LocationConfirmation> LocationConfirmations => Set<LocationConfirmation>();

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
    }
}
