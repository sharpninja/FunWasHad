using FWH.MarketingApi.Models;
using Microsoft.EntityFrameworkCore;

namespace FWH.MarketingApi.Data;

internal class MarketingDbContext : DbContext
{
    public MarketingDbContext(DbContextOptions<MarketingDbContext> options) : base(options)
    {
    }

    public DbSet<Business> Businesses => Set<Business>();
    public DbSet<BusinessTheme> BusinessThemes => Set<BusinessTheme>();
    public DbSet<Coupon> Coupons => Set<Coupon>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<NewsItem> NewsItems => Set<NewsItem>();
    public DbSet<Feedback> Feedbacks => Set<Feedback>();
    public DbSet<FeedbackAttachment> FeedbackAttachments => Set<FeedbackAttachment>();
    public DbSet<City> Cities => Set<City>();
    public DbSet<CityTheme> CityThemes => Set<CityTheme>();
    public DbSet<TourismMarket> TourismMarkets => Set<TourismMarket>();
    public DbSet<CityTourismMarket> CityTourismMarkets => Set<CityTourismMarket>();
    public DbSet<Airport> Airports => Set<Airport>();
    public DbSet<AirportTourismMarket> AirportTourismMarkets => Set<AirportTourismMarket>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Business configuration
        modelBuilder.Entity<Business>(entity =>
        {
            entity.ToTable("businesses");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Address).IsRequired().HasMaxLength(500);
            entity.Property(e => e.PhoneNumber).HasMaxLength(50);
            entity.Property(e => e.Email).HasMaxLength(200);
            entity.Property(e => e.Website).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now() at time zone 'utc'");

            entity.HasIndex(e => new { e.Latitude, e.Longitude });
            entity.HasIndex(e => e.IsSubscribed);
        });

        // BusinessTheme configuration
        modelBuilder.Entity<BusinessTheme>(entity =>
        {
            entity.ToTable("business_themes");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ThemeName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.PrimaryColor).HasMaxLength(20);
            entity.Property(e => e.SecondaryColor).HasMaxLength(20);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now() at time zone 'utc'");

            entity.HasOne(e => e.Business)
                .WithOne(b => b.Theme)
                .HasForeignKey<BusinessTheme>(e => e.BusinessId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Coupon configuration
        modelBuilder.Entity<Coupon>(entity =>
        {
            entity.ToTable("coupons");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).IsRequired();
            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now() at time zone 'utc'");

            entity.HasOne(e => e.Business)
                .WithMany(b => b.Coupons)
                .HasForeignKey(e => e.BusinessId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.BusinessId, e.IsActive });
            entity.HasIndex(e => new { e.ValidFrom, e.ValidUntil });
        });

        // MenuItem configuration
        modelBuilder.Entity<MenuItem>(entity =>
        {
            entity.ToTable("menu_items");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Category).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Currency).HasMaxLength(10);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now() at time zone 'utc'");

            entity.HasOne(e => e.Business)
                .WithMany(b => b.MenuItems)
                .HasForeignKey(e => e.BusinessId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.BusinessId, e.Category, e.IsAvailable });
        });

        // NewsItem configuration
        modelBuilder.Entity<NewsItem>(entity =>
        {
            entity.ToTable("news_items");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.Author).HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now() at time zone 'utc'");

            entity.HasOne(e => e.Business)
                .WithMany(b => b.NewsItems)
                .HasForeignKey(e => e.BusinessId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.BusinessId, e.IsPublished, e.PublishedAt });
        });

        // Feedback configuration
        modelBuilder.Entity<Feedback>(entity =>
        {
            entity.ToTable("feedback");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(200);
            entity.Property(e => e.UserName).HasMaxLength(200);
            entity.Property(e => e.UserEmail).HasMaxLength(200);
            entity.Property(e => e.FeedbackType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Subject).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Message).IsRequired();
            entity.Property(e => e.SubmittedAt).HasDefaultValueSql("now() at time zone 'utc'");

            entity.HasOne(e => e.Business)
                .WithMany(b => b.Feedback)
                .HasForeignKey(e => e.BusinessId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.BusinessId, e.SubmittedAt });
            entity.HasIndex(e => new { e.UserId, e.SubmittedAt });
            entity.HasIndex(e => new { e.IsPublic, e.IsApproved });
        });

        // FeedbackAttachment configuration
        modelBuilder.Entity<FeedbackAttachment>(entity =>
        {
            entity.ToTable("feedback_attachments");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AttachmentType).IsRequired().HasMaxLength(20);
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(500);
            entity.Property(e => e.ContentType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.StorageUrl).IsRequired();
            entity.Property(e => e.UploadedAt).HasDefaultValueSql("now() at time zone 'utc'");

            entity.HasOne(e => e.Feedback)
                .WithMany(f => f.Attachments)
                .HasForeignKey(e => e.FeedbackId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // City configuration
        modelBuilder.Entity<City>(entity =>
        {
            entity.ToTable("cities");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.State).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Country).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.Website).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now() at time zone 'utc'");

            entity.HasIndex(e => new { e.Name, e.State, e.Country });
            entity.HasIndex(e => e.IsActive);
        });

        // CityTheme configuration
        modelBuilder.Entity<CityTheme>(entity =>
        {
            entity.ToTable("city_themes");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ThemeName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.PrimaryColor).HasMaxLength(20);
            entity.Property(e => e.SecondaryColor).HasMaxLength(20);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now() at time zone 'utc'");

            entity.HasOne(e => e.City)
                .WithOne(c => c.Theme)
                .HasForeignKey<CityTheme>(e => e.CityId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // TourismMarket configuration
        modelBuilder.Entity<TourismMarket>(entity =>
        {
            entity.ToTable("tourism_markets");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now() at time zone 'utc'");

            entity.HasIndex(e => e.IsActive);
        });

        // CityTourismMarket configuration (join table)
        modelBuilder.Entity<CityTourismMarket>(entity =>
        {
            entity.ToTable("city_tourism_markets");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now() at time zone 'utc'");

            entity.HasOne(e => e.City)
                .WithMany(c => c.CityTourismMarkets)
                .HasForeignKey(e => e.CityId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.TourismMarket)
                .WithMany(t => t.CityTourismMarkets)
                .HasForeignKey(e => e.TourismMarketId)
                .OnDelete(DeleteBehavior.Cascade);

            // Ensure unique combination of City and TourismMarket
            entity.HasIndex(e => new { e.CityId, e.TourismMarketId }).IsUnique();
        });

        // Airport configuration
        modelBuilder.Entity<Airport>(entity =>
        {
            entity.ToTable("airports");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.IataCode).IsRequired().HasMaxLength(3);
            entity.Property(e => e.IcaoCode).HasMaxLength(4);
            entity.Property(e => e.City).IsRequired().HasMaxLength(200);
            entity.Property(e => e.State).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Country).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.Latitude).IsRequired();
            entity.Property(e => e.Longitude).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now() at time zone 'utc'");

            entity.HasIndex(e => e.IataCode);
            entity.HasIndex(e => e.IcaoCode);
            entity.HasIndex(e => new { e.Latitude, e.Longitude });
            entity.HasIndex(e => e.IsActive);
        });

        // AirportTourismMarket configuration (join table)
        modelBuilder.Entity<AirportTourismMarket>(entity =>
        {
            entity.ToTable("airport_tourism_markets");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now() at time zone 'utc'");

            entity.HasOne(e => e.Airport)
                .WithMany(a => a.AirportTourismMarkets)
                .HasForeignKey(e => e.AirportId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.TourismMarket)
                .WithMany(t => t.AirportTourismMarkets)
                .HasForeignKey(e => e.TourismMarketId)
                .OnDelete(DeleteBehavior.Cascade);

            // Ensure unique combination of Airport and TourismMarket
            entity.HasIndex(e => new { e.AirportId, e.TourismMarketId }).IsUnique();
        });
    }
}
