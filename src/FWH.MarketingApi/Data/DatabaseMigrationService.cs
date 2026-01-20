using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FWH.MarketingApi.Data;

/// <summary>
/// Service for applying SQL-based database migrations.
/// Reads .sql files from the Migrations folder and executes them in order.
/// </summary>
public class DatabaseMigrationService
{
    private readonly string _connectionString;
    private readonly ILogger<DatabaseMigrationService> _logger;

    public DatabaseMigrationService(string connectionString, ILogger<DatabaseMigrationService> logger)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Converts PostgreSQL URI format connection string to standard format if needed.
    /// NpgsqlConnectionStringBuilder doesn't support URI format directly.
    /// </summary>
    private string GetConnectionString()
    {
        var connectionString = _connectionString;
        if (connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase) ||
            connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                // Parse the URI manually
                var uri = new Uri(connectionString);
                var builder = new NpgsqlConnectionStringBuilder
                {
                    Host = uri.Host,
                    Port = uri.Port > 0 ? uri.Port : 5432,
                    Database = uri.AbsolutePath.TrimStart('/'),
                    Username = uri.UserInfo.Split(':')[0],
                    Password = uri.UserInfo.Contains(':')
                        ? Uri.UnescapeDataString(uri.UserInfo.Split(':', 2)[1])
                        : string.Empty
                };

                // Parse query string parameters if present (e.g., ?sslmode=require)
                if (!string.IsNullOrEmpty(uri.Query) && uri.Query.Length > 1)
                {
                    var query = uri.Query.Substring(1); // Remove leading '?'
                    var pairs = query.Split('&');
                    foreach (var pair in pairs)
                    {
                        var keyValue = pair.Split('=', 2);
                        if (keyValue.Length == 2 && !string.IsNullOrEmpty(keyValue[0]))
                        {
                            var key = Uri.UnescapeDataString(keyValue[0]);
                            var value = Uri.UnescapeDataString(keyValue[1]);
                            builder[key] = value;
                        }
                    }
                }

                connectionString = builder.ToString();
                _logger.LogDebug("Converted PostgreSQL URI to connection string format");
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

        return connectionString;
    }

    public async Task ApplyMigrationsAsync()
    {
        _logger.LogInformation("Starting database migration process");

        try
        {
            // Ensure database exists before attempting to connect to it
            await EnsureDatabaseExistsAsync();

            // Ensure migrations table exists
            await EnsureMigrationsTableAsync();

            // Get migration files from Migrations directory
            var migrationsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Migrations");

            if (!Directory.Exists(migrationsPath))
            {
                _logger.LogWarning("Migrations directory not found at {Path}, skipping migrations", migrationsPath);
                return;
            }

            var migrationFiles = Directory.GetFiles(migrationsPath, "*.sql")
                .OrderBy(f => Path.GetFileName(f))
                .ToList();

            if (migrationFiles.Count == 0)
            {
                _logger.LogInformation("No migration files found");
                return;
            }

            _logger.LogInformation("Found {Count} migration files", migrationFiles.Count);

            // Apply each migration
            foreach (var migrationFile in migrationFiles)
            {
                var migrationName = Path.GetFileNameWithoutExtension(migrationFile);

                if (await IsMigrationAppliedAsync(migrationName))
                {
                    _logger.LogDebug("Migration {Name} already applied, skipping", migrationName);
                    continue;
                }

                _logger.LogInformation("Applying migration: {Name}", migrationName);

                var sql = await File.ReadAllTextAsync(migrationFile);
                await ExecuteMigrationAsync(sql, migrationName);

                _logger.LogInformation("Successfully applied migration: {Name}", migrationName);
            }

            _logger.LogInformation("Database migration process completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply database migrations");
            throw;
        }
    }

    private async Task EnsureDatabaseExistsAsync()
    {
        // Get connection string (converts URI format if needed)
        var connectionString = GetConnectionString();
        var builder = new NpgsqlConnectionStringBuilder(connectionString);

        // If the connection string doesn't set a database explicitly, nothing to create.
        var targetDatabase = builder.Database;
        if (string.IsNullOrWhiteSpace(targetDatabase))
        {
            _logger.LogDebug("No database specified in connection string; skipping database creation check.");
            return;
        }

        // Connect to a known-existing maintenance DB to create/check the target DB.
        // 'postgres' is present on typical Postgres installations.
        builder.Database = "postgres";

        await using var adminConnection = new NpgsqlConnection(builder.ConnectionString);
        await adminConnection.OpenAsync();

        const string existsSql = "SELECT 1 FROM pg_database WHERE datname = @db";
        await using (var existsCommand = new NpgsqlCommand(existsSql, adminConnection))
        {
            existsCommand.Parameters.AddWithValue("db", targetDatabase);
            var exists = await existsCommand.ExecuteScalarAsync() is not null;

            if (exists)
            {
                _logger.LogInformation("Database '{Database}' already exists", targetDatabase);
                return;
            }
        }

        // CREATE DATABASE cannot be parameterized; quote identifier safely.
        var quotedDbName = '"' + targetDatabase.Replace("\"", "\"\"") + '"';
        var createSql = $"CREATE DATABASE {quotedDbName}";

        await using (var createCommand = new NpgsqlCommand(createSql, adminConnection))
        {
            await createCommand.ExecuteNonQueryAsync();
        }

        _logger.LogInformation("Database '{Database}' created", targetDatabase);
    }

    private async Task EnsureMigrationsTableAsync()
    {
        var sql = @"
            CREATE TABLE IF NOT EXISTS __migrations (
                id SERIAL PRIMARY KEY,
                migration_name VARCHAR(255) NOT NULL UNIQUE,
                applied_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT (now() at time zone 'utc')
            );
        ";

        var connectionString = GetConnectionString();
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private async Task<bool> IsMigrationAppliedAsync(string migrationName)
    {
        var sql = "SELECT COUNT(*) FROM __migrations WHERE migration_name = @name";

        var connectionString = GetConnectionString();
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("name", migrationName);

        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result) > 0;
    }

    private async Task ExecuteMigrationAsync(string sql, string migrationName)
    {
        var connectionString = GetConnectionString();
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            // Execute migration SQL
            await using (var command = new NpgsqlCommand(sql, connection, transaction))
            {
                await command.ExecuteNonQueryAsync();
            }

            // Record migration as applied
            var recordSql = "INSERT INTO __migrations (migration_name) VALUES (@name)";
            await using (var command = new NpgsqlCommand(recordSql, connection, transaction))
            {
                command.Parameters.AddWithValue("name", migrationName);
                await command.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
