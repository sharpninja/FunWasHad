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
/// Falls back to SQLite when containers are unavailable to keep tests runnable in constrained environments.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private enum DatabaseProvider
    {
        PostgreSql,
        Sqlite,
    }

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
    private DatabaseProvider _databaseProvider = DatabaseProvider.Sqlite;
    private string? _sqliteDbPath;
    private bool _containerStarted;
    private bool _useContainers;

    /// <summary>Root service provider from the built test server. Use CreateScope() for scoped services.</summary>
    public override IServiceProvider Services
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
#pragma warning disable EF1001
            services.RemoveAll<DbContextOptions<MarketingDbContext>>();
            services.RemoveAll<MarketingDbContext>();
            services.RemoveAll(typeof(Microsoft.EntityFrameworkCore.Internal.IDbContextPool<>));
            services.RemoveAll(typeof(Microsoft.EntityFrameworkCore.Internal.IDbContextPool<MarketingDbContext>));
            services.RemoveAll(typeof(Microsoft.EntityFrameworkCore.Internal.IScopedDbContextLease<>));
            services.RemoveAll(typeof(Microsoft.EntityFrameworkCore.Internal.IScopedDbContextLease<MarketingDbContext>));
#pragma warning restore EF1001

            // Register DbContext WITHOUT pooling for tests
            services.AddDbContext<MarketingDbContext>((sp, options) =>
            {
                switch (_databaseProvider)
                {
                    case DatabaseProvider.PostgreSql:
                        options.UseNpgsql(_connectionString ?? throw new InvalidOperationException("PostgreSQL container not started"), npgsqlOptions =>
                        {
                            npgsqlOptions.SetPostgresVersion(16, 0);
                        });
                        break;
                    case DatabaseProvider.Sqlite:
                        options.UseSqlite(_connectionString ?? throw new InvalidOperationException("SQLite fallback not initialized"));
                        break;
                    default:
                        throw new InvalidOperationException("Unsupported database provider for tests");
                }
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
                { "ConnectionStrings:marketing", _connectionString ?? throw new InvalidOperationException("Database not configured for tests") },
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
        if (_initialized)
        {
            return;
        }

        _useContainers = string.Equals(Environment.GetEnvironmentVariable("USE_TESTCONTAINERS"), "true", StringComparison.OrdinalIgnoreCase);

        if (_useContainers && await TryStartPostgresAsync().ConfigureAwait(true))
        {
            _databaseProvider = DatabaseProvider.PostgreSql;
        }
        else
        {
            _databaseProvider = DatabaseProvider.Sqlite;
            _sqliteDbPath = Path.Combine(Path.GetTempPath(), $"marketing-api-test-{Guid.NewGuid():N}.db");
            _connectionString = $"Data Source={_sqliteDbPath}";
        }

        _initialized = true;
    }

    public override async ValueTask DisposeAsync()
    {
        if (_databaseProvider == DatabaseProvider.PostgreSql && _containerStarted)
        {
            await _postgresContainer.DisposeAsync().ConfigureAwait(true);
        }

        if (_databaseProvider == DatabaseProvider.Sqlite && _sqliteDbPath is not null)
        {
            try
            {
                if (File.Exists(_sqliteDbPath))
                {
                    File.Delete(_sqliteDbPath);
                }
            }
            catch
            {
                // Ignore cleanup errors in tests
            }
        }

        await base.DisposeAsync().ConfigureAwait(true);
    }

    protected override void Dispose(bool disposing)
    {
        // Container disposal is handled by DisposeAsync
        base.Dispose(disposing);
    }

    private async Task<bool> TryStartPostgresAsync()
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(20));
            await _postgresContainer.StartAsync(cts.Token).ConfigureAwait(true);
            _connectionString = _postgresContainer.GetConnectionString();
            _containerStarted = true;
            _databaseProvider = DatabaseProvider.PostgreSql;
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"PostgreSQL test container unavailable, falling back to SQLite: {ex.Message}");
            return false;
        }
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
