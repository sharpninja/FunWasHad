using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace FWH.Location.Api.Data;

/// <summary>
/// Service for applying database migrations on application startup.
/// Executes SQL migration scripts from the Migrations folder in order.
/// </summary>
public class DatabaseMigrationService
{
    private readonly string _connectionString;
    private readonly ILogger<DatabaseMigrationService> _logger;

    public DatabaseMigrationService(
        string connectionString,
        ILogger<DatabaseMigrationService> logger)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Applies all pending migrations to the database.
    /// </summary>
    public async Task ApplyMigrationsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting database migration process");

            // Ensure database exists
            await EnsureDatabaseExistsAsync(cancellationToken);

            // Create migrations tracking table if it doesn't exist
            await EnsureMigrationsTableExistsAsync(cancellationToken);

            // Get applied migrations
            var appliedMigrations = await GetAppliedMigrationsAsync(cancellationToken);
            _logger.LogInformation("Found {Count} previously applied migrations", appliedMigrations.Count);

            // Get migration scripts from embedded resources or files
            var migrationScripts = GetMigrationScripts();
            _logger.LogInformation("Found {Count} migration script(s)", migrationScripts.Count);

            // Apply pending migrations
            var pendingMigrations = migrationScripts
                .Where(m => !appliedMigrations.Contains(m.Name))
                .OrderBy(m => m.Name)
                .ToList();

            if (!pendingMigrations.Any())
            {
                _logger.LogInformation("No pending migrations to apply");
                return;
            }

            _logger.LogInformation("Applying {Count} pending migration(s)", pendingMigrations.Count);

            foreach (var migration in pendingMigrations)
            {
                await ApplyMigrationAsync(migration, cancellationToken);
            }

            _logger.LogInformation("Database migration process completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during database migration");
            throw;
        }
    }

    /// <summary>
    /// Ensures the database exists. Creates it if it doesn't.
    /// </summary>
    private async Task EnsureDatabaseExistsAsync(CancellationToken cancellationToken)
    {
        // Handle PostgreSQL URI format (e.g., postgresql://user:pass@host:port/db)
        var connectionString = _connectionString;
        if (connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase) ||
            connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase))
        {
            // Npgsql can handle URI format, but let's ensure it's properly formatted
            try
            {
                var builder = new NpgsqlConnectionStringBuilder(connectionString);
                connectionString = builder.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse connection string URI format. Connection string starts with: {Prefix}",
                    connectionString.Substring(0, Math.Min(20, connectionString.Length)));
                throw new ArgumentException(
                    "Connection string is in URI format but cannot be parsed. " +
                    "Ensure Railway DATABASE_URL is properly formatted.", ex);
            }
        }

        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        var databaseName = builder.Database;

        // Connect to postgres database to check if target database exists
        builder.Database = "postgres";
        var adminConnectionString = builder.ToString();

        await using var connection = new NpgsqlConnection(adminConnectionString);
        await connection.OpenAsync(cancellationToken);

        // Check if database exists
        await using var checkCmd = new NpgsqlCommand(
            "SELECT 1 FROM pg_database WHERE datname = @databaseName",
            connection);
        checkCmd.Parameters.AddWithValue("databaseName", databaseName!);

        var exists = await checkCmd.ExecuteScalarAsync(cancellationToken) != null;

        if (!exists)
        {
            _logger.LogInformation("Database '{Database}' does not exist. Creating it...", databaseName);

            // Create database
            await using var createCmd = new NpgsqlCommand(
                $"CREATE DATABASE {databaseName}",
                connection);
            await createCmd.ExecuteNonQueryAsync(cancellationToken);

            _logger.LogInformation("Database '{Database}' created successfully", databaseName);
        }
        else
        {
            _logger.LogInformation("Database '{Database}' already exists", databaseName);
        }
    }

    /// <summary>
    /// Creates the migrations tracking table if it doesn't exist.
    /// </summary>
    private async Task EnsureMigrationsTableExistsAsync(CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var createTableSql = @"
            CREATE TABLE IF NOT EXISTS __migrations (
                id SERIAL PRIMARY KEY,
                migration_name VARCHAR(255) NOT NULL UNIQUE,
                applied_at TIMESTAMPTZ NOT NULL DEFAULT (now() AT TIME ZONE 'utc')
            )";

        await using var command = new NpgsqlCommand(createTableSql, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);

        _logger.LogDebug("Migrations tracking table verified");
    }

    /// <summary>
    /// Gets the list of already applied migrations.
    /// </summary>
    private async Task<HashSet<string>> GetAppliedMigrationsAsync(CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var sql = "SELECT migration_name FROM __migrations ORDER BY id";
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var migrations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (await reader.ReadAsync(cancellationToken))
        {
            migrations.Add(reader.GetString(0));
        }

        return migrations;
    }

    /// <summary>
    /// Gets migration scripts from the Migrations folder.
    /// </summary>
    private List<MigrationScript> GetMigrationScripts()
    {
        var migrations = new List<MigrationScript>();
        var migrationsPath = Path.Combine(AppContext.BaseDirectory, "Migrations");

        if (!Directory.Exists(migrationsPath))
        {
            _logger.LogWarning("Migrations directory not found at: {Path}", migrationsPath);
            return migrations;
        }

        var sqlFiles = Directory.GetFiles(migrationsPath, "*.sql")
            .OrderBy(f => f)
            .ToList();

        foreach (var file in sqlFiles)
        {
            var fileName = Path.GetFileName(file);
            var content = File.ReadAllText(file);

            migrations.Add(new MigrationScript
            {
                Name = fileName,
                Content = content
            });
        }

        return migrations;
    }

    /// <summary>
    /// Applies a single migration script.
    /// </summary>
    private async Task ApplyMigrationAsync(MigrationScript migration, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Applying migration: {Name}", migration.Name);

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        // Start transaction
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            // Execute migration script
            await using var command = new NpgsqlCommand(migration.Content, connection, transaction);
            await command.ExecuteNonQueryAsync(cancellationToken);

            // Record migration as applied
            await using var recordCmd = new NpgsqlCommand(
                "INSERT INTO __migrations (migration_name) VALUES (@name)",
                connection,
                transaction);
            recordCmd.Parameters.AddWithValue("name", migration.Name);
            await recordCmd.ExecuteNonQueryAsync(cancellationToken);

            // Commit transaction
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation("Migration '{Name}' applied successfully", migration.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying migration '{Name}'", migration.Name);
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private class MigrationScript
    {
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }
}
