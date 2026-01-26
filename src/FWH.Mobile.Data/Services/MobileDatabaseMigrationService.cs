using FWH.Mobile.Data.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FWH.Mobile.Data.Services;

/// <summary>
/// Service for managing SQLite database migrations in the mobile app.
/// Ensures database schema is up-to-date with the latest migrations.
/// </summary>
public partial class MobileDatabaseMigrationService
{
    [LoggerMessage(LogLevel.Information, "Checking mobile database status...")]
    private static partial void LogCheckingStatus(ILogger logger);

    [LoggerMessage(LogLevel.Information, "Creating mobile database...")]
    private static partial void LogCreating(ILogger logger);

    [LoggerMessage(LogLevel.Information, "Mobile database created successfully")]
    private static partial void LogCreated(ILogger logger);

    [LoggerMessage(LogLevel.Information, "Mobile database is up to date")]
    private static partial void LogUpToDate(ILogger logger);

    [LoggerMessage(LogLevel.Information, "Applying {Count} pending migrations to mobile database")]
    private static partial void LogApplyingMigrations(ILogger logger, int count);

    [LoggerMessage(LogLevel.Debug, "Pending migration: {Migration}")]
    private static partial void LogPendingMigration(ILogger logger, string migration);

    [LoggerMessage(LogLevel.Information, "Mobile database migrations applied successfully")]
    private static partial void LogMigrationsApplied(ILogger logger);

    [LoggerMessage(LogLevel.Error, "Error ensuring mobile database: {Message}")]
    private static partial void LogEnsureError(ILogger logger, Exception ex, string message);

    [LoggerMessage(LogLevel.Error, "Error getting applied migrations: {Message}")]
    private static partial void LogGetAppliedError(ILogger logger, Exception ex, string message);

    [LoggerMessage(LogLevel.Error, "Error getting pending migrations: {Message}")]
    private static partial void LogGetPendingError(ILogger logger, Exception ex, string message);

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
            LogCheckingStatus(_logger);

            // Check if database exists
            var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken).ConfigureAwait(false);

            if (!canConnect)
            {
                LogCreating(_logger);
                await _dbContext.Database.EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);
                LogCreated(_logger);
                return;
            }

            // Check for pending migrations
            var pendingMigrations = await _dbContext.Database.GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(false);
            var pendingList = pendingMigrations.ToList();

            if (pendingList.Count == 0)
            {
                LogUpToDate(_logger);
                return;
            }

            LogApplyingMigrations(_logger, pendingList.Count);
            foreach (var migration in pendingList)
            {
                LogPendingMigration(_logger, migration);
            }

            // Apply migrations
            await _dbContext.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
            LogMigrationsApplied(_logger);
        }
        catch (Exception ex)
        {
            LogEnsureError(_logger, ex, ex.Message);
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
            var applied = await _dbContext.Database.GetAppliedMigrationsAsync(cancellationToken).ConfigureAwait(false);
            return applied;
        }
        catch (Exception ex)
        {
            LogGetAppliedError(_logger, ex, ex.Message);
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
            var pending = await _dbContext.Database.GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(false);
            return pending;
        }
        catch (Exception ex)
        {
            LogGetPendingError(_logger, ex, ex.Message);
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
