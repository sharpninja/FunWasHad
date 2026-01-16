using FWH.Mobile.Data.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FWH.Mobile.Data.Services;

/// <summary>
/// Service for managing SQLite database migrations in the mobile app.
/// Ensures database schema is up-to-date with the latest migrations.
/// </summary>
public class MobileDatabaseMigrationService
{
    private readonly NotesDbContext _dbContext;
    private readonly ILogger<MobileDatabaseMigrationService> _logger;

    public MobileDatabaseMigrationService(
        NotesDbContext dbContext,
        ILogger<MobileDatabaseMigrationService> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Ensures the database exists and applies any pending migrations.
    /// Safe to call multiple times.
    /// </summary>
    public async Task EnsureDatabaseAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Checking mobile database status...");

            // Check if database exists
            var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);

            if (!canConnect)
            {
                _logger.LogInformation("Creating mobile database...");
                await _dbContext.Database.EnsureCreatedAsync(cancellationToken);
                _logger.LogInformation("Mobile database created successfully");
                return;
            }

            // Check for pending migrations
            var pendingMigrations = await _dbContext.Database.GetPendingMigrationsAsync(cancellationToken);
            var pendingList = pendingMigrations.ToList();

            if (pendingList.Count == 0)
            {
                _logger.LogInformation("Mobile database is up to date");
                return;
            }

            _logger.LogInformation("Applying {Count} pending migrations to mobile database", pendingList.Count);
            foreach (var migration in pendingList)
            {
                _logger.LogDebug("Pending migration: {Migration}", migration);
            }

            // Apply migrations
            await _dbContext.Database.MigrateAsync(cancellationToken);
            _logger.LogInformation("Mobile database migrations applied successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ensuring mobile database: {Message}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Gets the list of applied migrations.
    /// </summary>
    public async Task<IEnumerable<string>> GetAppliedMigrationsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var applied = await _dbContext.Database.GetAppliedMigrationsAsync(cancellationToken);
            return applied;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting applied migrations: {Message}", ex.Message);
            return Enumerable.Empty<string>();
        }
    }

    /// <summary>
    /// Gets the list of pending migrations.
    /// </summary>
    public async Task<IEnumerable<string>> GetPendingMigrationsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var pending = await _dbContext.Database.GetPendingMigrationsAsync(cancellationToken);
            return pending;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending migrations: {Message}", ex.Message);
            return Enumerable.Empty<string>();
        }
    }

    /// <summary>
    /// Gets database connection information (for diagnostics).
    /// </summary>
    public string GetConnectionInfo()
    {
        var connection = _dbContext.Database.GetConnectionString();
        return connection ?? "No connection string configured";
    }
}
