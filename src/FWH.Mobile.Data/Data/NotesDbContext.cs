using Microsoft.EntityFrameworkCore;
using FWH.Mobile.Data.Models;
using FWH.Mobile.Data.Entities;

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

    public NotesDbContext(DbContextOptions<NotesDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
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
    }
}
