using FWH.MarketingApi.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Testcontainers.PostgreSql;
using Xunit;

namespace FWH.MarketingApi.Tests;

/// <summary>
/// Custom web application factory for Marketing API integration tests.
/// Implements TR-TEST-002: Integration Tests - API endpoints.
///
/// Uses PostgreSQL test container to match production database and support
/// all EF Core features including filtered includes and navigation property filters.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("marketing_test")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    private string? _connectionString;

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Replace database with PostgreSQL test container
            // Remove ALL existing DbContext registrations including pooling
            services.RemoveAll<DbContextOptions<MarketingDbContext>>();
            services.RemoveAll<MarketingDbContext>();
            services.RemoveAll(typeof(Microsoft.EntityFrameworkCore.Internal.IDbContextPool<>));
            services.RemoveAll(typeof(Microsoft.EntityFrameworkCore.Internal.IDbContextPool<MarketingDbContext>));
            services.RemoveAll(typeof(Microsoft.EntityFrameworkCore.Internal.IScopedDbContextLease<>));
            services.RemoveAll(typeof(Microsoft.EntityFrameworkCore.Internal.IScopedDbContextLease<MarketingDbContext>));

            // Register DbContext WITHOUT pooling for tests
            services.AddDbContext<MarketingDbContext>((sp, options) =>
            {
                options.UseNpgsql(_connectionString ?? throw new InvalidOperationException("PostgreSQL container not started"), npgsqlOptions =>
                {
                    npgsqlOptions.SetPostgresVersion(16, 0);
                });
                // Enable sensitive data logging for debugging
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }, ServiceLifetime.Scoped, ServiceLifetime.Scoped); // Explicitly set to Scoped, not Singleton

            // Replace the implementation with test-specific one
            services.RemoveAll<MarketingDbContext>();
            services.AddScoped<MarketingDbContext>(sp =>
            {
                var options = sp.GetRequiredService<DbContextOptions<MarketingDbContext>>();
                return new TestMarketingDbContext(options);
            });
        });

        // Configure connection string and blob storage for tests
        var testUploadsPath = Path.Combine(Path.GetTempPath(), $"marketing-api-test-uploads-{Guid.NewGuid()}");
        Directory.CreateDirectory(testUploadsPath);

        builder.ConfigureAppConfiguration(config =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ConnectionStrings:marketing", _connectionString ?? throw new InvalidOperationException("PostgreSQL container not started") },
                { "BlobStorage:Provider", "LocalFile" },
                { "BlobStorage:LocalPath", testUploadsPath },
                { "BlobStorage:BaseUrl", "/uploads" },
                { "ApiSecurity:ApiKey", "test-api-key" },
                { "ApiSecurity:ApiSecret", "test-api-secret" },
                { "ApiSecurity:RequireAuthentication", "false" } // Disable auth for easier testing
            });
        });

        var host = base.CreateHost(builder);

        // Ensure database schema is created using migrations
        using (var scope = host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MarketingDbContext>();
            var logger = scope.ServiceProvider.GetService<ILogger<CustomWebApplicationFactory>>();

            try
            {
                // Drop and recreate database to ensure clean state
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();

                logger?.LogInformation("Database schema created successfully");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error ensuring database created");
                throw;
            }
        }

        return host;
    }

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();
        _connectionString = _postgresContainer.GetConnectionString();
    }

    public new async Task DisposeAsync()
    {
        await _postgresContainer.DisposeAsync();
        await base.DisposeAsync();
    }

    protected override void Dispose(bool disposing)
    {
        // Container disposal is handled by DisposeAsync
        base.Dispose(disposing);
    }
}
