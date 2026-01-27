using FWH.MarketingApi.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace FWH.MarketingApi.Tests;

/// <summary>
/// Test-specific DbContext that uses explicit lowercase column name mapping
/// to match PostgreSQL's default behavior of converting unquoted identifiers to lowercase.
/// </summary>
internal class TestMarketingDbContext : MarketingDbContext
{
    public TestMarketingDbContext(DbContextOptions<MarketingDbContext> options)
        : base(options)
    {
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        EnforceUniqueConstraints();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        EnforceUniqueConstraints();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void EnforceUniqueConstraints()
    {
        EnforceUniqueCombination(CityTourismMarkets, e => new { e.CityId, e.TourismMarketId }, "Duplicate CityTourismMarket relationship");
        EnforceUniqueCombination(AirportTourismMarkets, e => new { e.AirportId, e.TourismMarketId }, "Duplicate AirportTourismMarket relationship");
    }

    private void EnforceUniqueCombination<TEntity, TKey>(DbSet<TEntity> set, Func<TEntity, TKey> keySelector, string message)
        where TEntity : class
        where TKey : notnull
    {
        var pending = ChangeTracker.Entries<TEntity>()
            .Where(e => e.State == EntityState.Added)
            .Select(e => e.Entity)
            .ToList();

        var existingKeys = set.AsEnumerable().Select(keySelector).ToHashSet();

        foreach (var entity in pending)
        {
            var key = keySelector(entity);
            var existsInStore = existingKeys.Contains(key);
            var existsInPending = pending.Any(other => !ReferenceEquals(other, entity) && EqualityComparer<TKey>.Default.Equals(keySelector(other), key));
            if (existsInStore || existsInPending)
            {
                throw new DbUpdateException(message, innerException: null);
            }
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Use test-specific configuration with explicit lowercase column names
        TestMarketingDbContextConfiguration.ConfigureForTests(modelBuilder);
    }
}
