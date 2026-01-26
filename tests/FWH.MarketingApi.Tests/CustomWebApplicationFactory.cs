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
    private IServiceProvider? _services;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _initialized;

    /// <summary>Root service provider from the built test server. Use CreateScope() for scoped services.</summary>
    public IServiceProvider Services
    {
        get
        {
            // Ensure InitializeAsync has been called and server is created
            // IClassFixture doesn't automatically call IAsyncLifetime.InitializeAsync()
            if (!_initialized)
            {
                _initLock.Wait();
                try
                {
                    if (!_initialized)
                    {
                        // InitializeAsync must be called before server can be created
                        // Since IClassFixture doesn't call it automatically, we need to call it synchronously
                        // This is a workaround - ideally xUnit would call InitializeAsync for IClassFixture
                        InitializeAsync().AsTask().GetAwaiter().GetResult();
                        _initialized = true;
                    }
                }
                finally
                {
                    _initLock.Release();
                }
            }
            
            // Ensure server is created - accessing Server property will trigger creation
            // Note: This may fail if WebApplicationFactory can't find CreateWebHostBuilder
            // For minimal hosting, we need to ensure CreateWebHostBuilder is overridden
            if (_services == null)
            {
                try
                {
                    _ = Server; // Trigger server creation
                    _services = Server.Host.Services;
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("CreateWebHostBuilder"))
                {
                    // If CreateWebHostBuilder is missing, try to create host manually
                    // This shouldn't happen if CreateWebHostBuilder is properly overridden
                    throw new InvalidOperationException(
                        "WebApplicationFactory failed to create server. Ensure CreateWebHostBuilder is properly overridden for minimal hosting model.", ex);
                }
            }
            return _services;
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Ensure InitializeAsync has been called before configuring the host
        // This is needed because IClassFixture doesn't automatically call IAsyncLifetime.InitializeAsync()
        if (!_initialized)
        {
            _initLock.Wait();
            try
            {
                if (!_initialized)
                {
                    InitializeAsync().AsTask().GetAwaiter().GetResult();
                    _initialized = true;
                }
            }
            finally
            {
                _initLock.Release();
            }
        }

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

            // Ensure database schema is created when host starts
            services.AddSingleton<IHostedService, EnsureDbCreatedHostedService>();
        });

        // Configure connection string and blob storage for tests
        var testUploadsPath = Path.Combine(Path.GetTempPath(), $"marketing-api-test-uploads-{Guid.NewGuid()}");
        Directory.CreateDirectory(testUploadsPath);

        builder.ConfigureAppConfiguration((_, config) =>
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
    }

    public async ValueTask InitializeAsync()
    {
        await _postgresContainer.StartAsync().ConfigureAwait(true);
        _connectionString = _postgresContainer.GetConnectionString();
    }

    public async ValueTask DisposeAsync()
    {
        await _postgresContainer.DisposeAsync().ConfigureAwait(true);
    }

    protected override void Dispose(bool disposing)
    {
        // Container disposal is handled by DisposeAsync
        base.Dispose(disposing);
    }

    private sealed class EnsureDbCreatedHostedService : IHostedService
    {
        private readonly IServiceProvider _services;

        public EnsureDbCreatedHostedService(IServiceProvider services) => _services = services;

        public Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = _services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<MarketingDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

}
