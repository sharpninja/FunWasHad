using Npgsql;

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

    public async Task ApplyMigrationsAsync()
    {
        _logger.LogInformation("Starting database migration process");

        try
        {
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

    private async Task EnsureMigrationsTableAsync()
    {
        var sql = @"
            CREATE TABLE IF NOT EXISTS __migrations (
                id SERIAL PRIMARY KEY,
                migration_name VARCHAR(255) NOT NULL UNIQUE,
                applied_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT (now() at time zone 'utc')
            );
        ";

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private async Task<bool> IsMigrationAppliedAsync(string migrationName)
    {
        var sql = "SELECT COUNT(*) FROM __migrations WHERE migration_name = @name";

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("name", migrationName);

        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result) > 0;
    }

    private async Task ExecuteMigrationAsync(string sql, string migrationName)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
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
