using FWH.MarketingApi.Data;
using Microsoft.EntityFrameworkCore;

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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Use test-specific configuration with explicit lowercase column names
        TestMarketingDbContextConfiguration.ConfigureForTests(modelBuilder);
    }
}
