using Npgsql;

namespace FWH.Location.Api.Data;

/// <summary>
/// Service for applying database migrations on application startup.
/// Executes SQL migration scripts from the Migrations folder in order.
/// </summary>
internal class DatabaseMigrationService
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
                    Password = uri.UserInfo.Contains(":", StringComparison.Ordinal)
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

    /// <summary>
    /// Applies all pending migrations to the database.
    /// </summary>
    public async Task ApplyMigrationsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Starting database migration process");

            // Ensure database exists
            await EnsureDatabaseExistsAsync(cancellationToken).ConfigureAwait(false);

            // Create migrations tracking table if it doesn't exist
            await EnsureMigrationsTableExistsAsync(cancellationToken).ConfigureAwait(false);

            // Get applied migrations
            var appliedMigrations = await GetAppliedMigrationsAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogDebug("Found {Count} previously applied migrations", appliedMigrations.Count);

            // Get migration scripts from embedded resources or files
            var migrationScripts = GetMigrationScripts();
            _logger.LogDebug("Found {Count} migration script(s)", migrationScripts.Count);

            // Apply pending migrations
            var pendingMigrations = migrationScripts
                .Where(m => !appliedMigrations.Contains(m.Name))
                .OrderBy(m => m.Name)
                .ToList();

            if (!pendingMigrations.Any())
            {
                _logger.LogDebug("No pending migrations to apply");
                return;
            }

            _logger.LogInformation("Applying {Count} pending migration(s)", pendingMigrations.Count);

            foreach (var migration in pendingMigrations)
            {
                await ApplyMigrationAsync(migration, cancellationToken).ConfigureAwait(false);
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
        // Get connection string (converts URI format if needed)
        var connectionString = GetConnectionString();
        var connectionStringBuilder = new NpgsqlConnectionStringBuilder(connectionString);
        var databaseName = connectionStringBuilder.Database;

        // Connect to postgres database to check if target database exists
        connectionStringBuilder.Database = "postgres";
        var adminConnectionString = connectionStringBuilder.ToString();

        await using var connection = new NpgsqlConnection(adminConnectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        // Check if database exists
        await using var checkCmd = new NpgsqlCommand(
            "SELECT 1 FROM pg_database WHERE datname = @databaseName",
            connection);
        checkCmd.Parameters.AddWithValue("databaseName", databaseName!);

        var exists = await checkCmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) != null;

        if (!exists)
        {
            _logger.LogInformation("Database '{Database}' does not exist. Creating it...", databaseName);

            // Create database
            await using var createCmd = new NpgsqlCommand(
                $"CREATE DATABASE {databaseName}",
                connection);
            await createCmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

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
        var connectionString = GetConnectionString();
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var createTableSql = @"
            CREATE TABLE IF NOT EXISTS __migrations (
                id SERIAL PRIMARY KEY,
                migration_name VARCHAR(255) NOT NULL UNIQUE,
                applied_at TIMESTAMPTZ NOT NULL DEFAULT (now() AT TIME ZONE 'utc')
            )";

        await using var command = new NpgsqlCommand(createTableSql, connection);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogDebug("Migrations tracking table verified");
    }

    /// <summary>
    /// Gets the list of already applied migrations.
    /// </summary>
    private async Task<HashSet<string>> GetAppliedMigrationsAsync(CancellationToken cancellationToken)
    {
        var connectionString = GetConnectionString();
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var sql = "SELECT migration_name FROM __migrations ORDER BY id";
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        var migrations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
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

        var connectionString = GetConnectionString();
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        // Start transaction
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            // Execute migration script
            await using var command = new NpgsqlCommand(migration.Content, connection, transaction);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

            // Record migration as applied
            await using var recordCmd = new NpgsqlCommand(
                "INSERT INTO __migrations (migration_name) VALUES (@name)",
                connection,
                transaction);
            recordCmd.Parameters.AddWithValue("name", migration.Name);
            await recordCmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

            // Commit transaction
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Migration '{Name}' applied successfully", migration.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying migration '{Name}'", migration.Name);
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    private class MigrationScript
    {
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }
}
