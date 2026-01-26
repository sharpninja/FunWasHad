using FWH.Mobile.Data.Entities;
using FWH.Mobile.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace FWH.Mobile.Data.Data;

public class NotesDbContext : DbContext
{
    public DbSet<Note> Notes { get; set; } = null!;

    public DbSet<WorkflowDefinitionEntity> WorkflowDefinitions { get; set; } = null!;
    public DbSet<NodeEntity> NodeEntities { get; set; } = null!;
    public DbSet<TransitionEntity> TransitionEntities { get; set; } = null!;
    public DbSet<StartPointEntity> StartPointEntities { get; set; } = null!;

    public DbSet<ConfigurationSetting> ConfigurationSettings { get; set; } = null!;

    /// <summary>
    /// Device location history - tracked locally, never sent to API
    /// TR-MOBILE-001: Local-only device location tracking
    /// </summary>
    public DbSet<DeviceLocationEntity> DeviceLocationHistory { get; set; } = null!;

    /// <summary>
    /// Places where the user became stationary - stores business information when available
    /// </summary>
    public DbSet<StationaryPlaceEntity> StationaryPlaces { get; set; } = null!;

    /// <summary>
    /// Cached city marketing information
    /// </summary>
    public DbSet<CityMarketingInfoEntity> CityMarketingInfo { get; set; } = null!;

    /// <summary>
    /// Stored images and logos
    /// </summary>
    public DbSet<ImageEntity> Images { get; set; } = null!;

    public NotesDbContext(DbContextOptions<NotesDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.Entity<Note>(b =>
        {
            b.HasKey(n => n.Id);
            b.Property(n => n.Title).IsRequired();
            b.Property(n => n.Content).IsRequired();
            b.Property(n => n.CreatedAt).IsRequired();
        });

        modelBuilder.Entity<WorkflowDefinitionEntity>(b =>
        {
            b.HasKey(w => w.Id);
            b.Property(w => w.Name).IsRequired();
            b.Property(w => w.CreatedAt).IsRequired();
            b.HasMany(w => w.Nodes).WithOne(n => n.WorkflowDefinition).HasForeignKey(n => n.WorkflowDefinitionEntityId).OnDelete(DeleteBehavior.Cascade);
            b.HasMany(w => w.Transitions).WithOne(t => t.WorkflowDefinition).HasForeignKey(t => t.WorkflowDefinitionEntityId).OnDelete(DeleteBehavior.Cascade);
            b.HasMany(w => w.StartPoints).WithOne(s => s.WorkflowDefinition).HasForeignKey(s => s.WorkflowDefinitionEntityId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<NodeEntity>(b =>
        {
            b.HasKey(n => n.Id);
            b.Property(n => n.NodeId).IsRequired();
            b.Property(n => n.Text).IsRequired();
        });

        modelBuilder.Entity<TransitionEntity>(b =>
        {
            b.HasKey(t => t.Id);
            b.Property(t => t.FromNodeId).IsRequired();
            b.Property(t => t.ToNodeId).IsRequired();
        });

        modelBuilder.Entity<StartPointEntity>(b =>
        {
            b.HasKey(s => s.Id);
        });

        modelBuilder.Entity<ConfigurationSetting>(b =>
        {
            b.HasKey(c => c.Key);
            b.Property(c => c.Key).IsRequired().HasMaxLength(200);
            b.Property(c => c.Value).IsRequired();
            b.Property(c => c.ValueType).IsRequired().HasMaxLength(50);
            b.Property(c => c.Category).HasMaxLength(100);
            b.Property(c => c.Description).HasMaxLength(500);
            b.Property(c => c.UpdatedAt).IsRequired();
            b.HasIndex(c => c.Category);
        });

        modelBuilder.Entity<DeviceLocationEntity>(b =>
        {
            b.HasKey(l => l.Id);
            b.Property(l => l.DeviceId).IsRequired().HasMaxLength(100);
            b.Property(l => l.Latitude).IsRequired();
            b.Property(l => l.Longitude).IsRequired();
            b.Property(l => l.MovementState).IsRequired().HasMaxLength(20);
            b.Property(l => l.Timestamp).IsRequired();
            b.Property(l => l.CreatedAt).IsRequired();
            b.Property(l => l.Address).HasMaxLength(500);

            // Indexes for efficient querying
            b.HasIndex(l => l.DeviceId);
            b.HasIndex(l => l.Timestamp);
            b.HasIndex(l => new { l.DeviceId, l.Timestamp });
        });

        modelBuilder.Entity<StationaryPlaceEntity>(b =>
        {
            b.HasKey(p => p.Id);
            b.Property(p => p.DeviceId).IsRequired().HasMaxLength(100);
            b.Property(p => p.Latitude).IsRequired();
            b.Property(p => p.Longitude).IsRequired();
            b.Property(p => p.StationaryAt).IsRequired();
            b.Property(p => p.CreatedAt).IsRequired();
            b.Property(p => p.BusinessName).HasMaxLength(200);
            b.Property(p => p.Address).HasMaxLength(500);
            b.Property(p => p.Category).HasMaxLength(100);
            b.Property(p => p.IsFavorite).IsRequired().HasDefaultValue(false);
            b.Property(p => p.LogoUrl).HasMaxLength(500);
            b.Property(p => p.PrimaryColor).HasMaxLength(20);
            b.Property(p => p.SecondaryColor).HasMaxLength(20);
            b.Property(p => p.AccentColor).HasMaxLength(20);
            b.Property(p => p.BackgroundColor).HasMaxLength(20);
            b.Property(p => p.TextColor).HasMaxLength(20);
            b.Property(p => p.BackgroundImageUrl).HasMaxLength(500);

            // Indexes for efficient querying (reverse chronological)
            b.HasIndex(p => p.DeviceId);
            b.HasIndex(p => p.StationaryAt).IsDescending();
            b.HasIndex(p => new { p.DeviceId, p.StationaryAt }).IsDescending();
            b.HasIndex(p => new { p.DeviceId, p.IsFavorite });
        });
    }
}
