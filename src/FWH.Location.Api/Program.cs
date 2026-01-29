using FWH.Common.Chat.Services;
using FWH.Common.Location.Configuration;
using FWH.Common.Location.Extensions;
using FWH.Location.Api.Data;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to listen on all interfaces (0.0.0.0) in Development
// This allows Android devices and emulators to connect via host IP
if (builder.Environment.IsDevelopment())
{
    builder.WebHost.ConfigureKestrel(options =>
    {
        // Listen on all interfaces for HTTP (port configured by Aspire: 4748)
        options.ListenAnyIP(4748);

        // Listen on all interfaces for HTTPS (port configured by Aspire: 4747)
        options.ListenAnyIP(4747, listenOptions =>
        {
            listenOptions.UseHttps();
        });
    });
}

// Add Aspire service defaults (telemetry, health checks, resilience)
builder.AddServiceDefaults();

// Add PostgreSQL with Aspire (skip in Test environment to allow test factory override)
if (!builder.Environment.IsEnvironment("Test"))
{
    builder.AddNpgsqlDbContext<LocationDbContext>("funwashad");
}

builder.Services.AddControllers();

// Add Swagger/OpenAPI for Debug and Staging builds
#if DEBUG || STAGING
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "FunWasHad Location API",
        Version = "v1",
        Description = "REST API for business discovery and location confirmations. Implements TR-API-005: Location API Endpoints."
    });

    // Include XML comments in Swagger documentation
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});
#endif

var locationOptions = builder.Configuration.GetSection("LocationService").Get<LocationServiceOptions>() ?? new();

// Register IPlatformService before adding location services
builder.Services.AddSingleton<IPlatformService, PlatformService>();

builder.Services.AddLocationServicesWithInMemoryConfig(options =>
{
    options.DefaultRadiusMeters = locationOptions.DefaultRadiusMeters;
    options.MaxRadiusMeters = locationOptions.MaxRadiusMeters;
    options.MinRadiusMeters = locationOptions.MinRadiusMeters;
    options.TimeoutSeconds = locationOptions.TimeoutSeconds;
    options.UserAgent = locationOptions.UserAgent;
    options.OverpassApiUrl = locationOptions.OverpassApiUrl;
});

var app = builder.Build();

// Apply database migrations on startup (skip in Test environment)
if (!app.Environment.IsEnvironment("Test"))
{
    await ApplyDatabaseMigrationsAsync(app).ConfigureAwait(false);
}

// Map Aspire default endpoints (health checks, metrics)
app.MapDefaultEndpoints();

// Enable Swagger UI for Debug and Staging builds
#if DEBUG || STAGING
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "FunWasHad Location API v1");
    options.RoutePrefix = "swagger";
    options.DisplayRequestDuration();
});
#endif

// Add API key authentication middleware
// Note: In Development/Staging, authentication is optional (can be disabled via config)
var requireAuth = app.Configuration.GetValue<bool>("ApiSecurity:RequireAuthentication", defaultValue: true);
if (requireAuth)
{
    app.UseMiddleware<FWH.Location.Api.Middleware.ApiKeyAuthenticationMiddleware>();
}

// HTTPS redirection disabled for Android development
// Android emulator connects via HTTP (http://10.0.2.2:4748)
// Enable in production with proper SSL certificates
// app.UseHttpsRedirection();

app.MapControllers();

static async Task ApplyDatabaseMigrationsAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogDebug("Checking for database migrations...");

        // Get connection string from configuration
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var connectionString = configuration.GetConnectionString("funwashad");

        // Log connection string status (without exposing sensitive data)
        if (string.IsNullOrEmpty(connectionString))
        {
            logger.LogError("Database connection string 'funwashad' is null or empty");

            // Try to get raw value to help diagnose
            var rawValue = configuration["ConnectionStrings:funwashad"];
            if (string.IsNullOrEmpty(rawValue))
            {
                logger.LogError("ConnectionStrings:funwashad configuration key is not set");
                logger.LogWarning("Available connection string keys: {Keys}",
                    string.Join(", ", configuration.GetSection("ConnectionStrings").GetChildren().Select(c => c.Key)));
            }
            else
            {
                logger.LogWarning("Connection string value appears to be: {Value} (may be unresolved Railway template)",
                    rawValue.Length > 50 ? rawValue.Substring(0, 50) + "..." : rawValue);
            }

            throw new InvalidOperationException(
                "Database connection string 'funwashad' is required. " +
                "In Railway, ensure ConnectionStrings__funwashad is set to ${{Postgres.DATABASE_URL}} " +
                "where 'Postgres' matches your PostgreSQL service name.");
        }

        // Validate connection string format
        if (connectionString.StartsWith("${{", StringComparison.Ordinal) || connectionString.Contains("{{", StringComparison.Ordinal))
        {
            logger.LogError("Connection string appears to be an unresolved Railway template: {Value}",
                connectionString.Substring(0, Math.Min(100, connectionString.Length)));
            throw new InvalidOperationException(
                "Connection string is an unresolved Railway template. " +
                "Ensure the PostgreSQL service name in Railway matches the reference in ConnectionStrings__funwashad. " +
                "Example: ConnectionStrings__funwashad=${{Postgres.DATABASE_URL}}");
        }

        // Log connection string format (without sensitive data)
        var connectionStringPreview = connectionString.Length > 50
            ? connectionString.Substring(0, 50) + "..."
            : connectionString;
        var isUriFormat = connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase) ||
                          connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase);
        logger.LogDebug("Connection string found (length: {Length} characters, format: {Format})",
            connectionString.Length,
            isUriFormat ? "URI" : "Connection String");

        // Note: We don't validate by parsing here because DatabaseMigrationService handles URI format conversion.
        // URI format connection strings will be converted to standard format in DatabaseMigrationService.
        if (isUriFormat)
        {
            logger.LogDebug("Connection string is in URI format - will be converted by DatabaseMigrationService");
        }

        // Create and run migration service
        var migrationLogger = scope.ServiceProvider.GetRequiredService<ILogger<DatabaseMigrationService>>();
        var migrationService = new DatabaseMigrationService(connectionString, migrationLogger);

        await migrationService.ApplyMigrationsAsync().ConfigureAwait(false);

        logger.LogInformation("Database migrations completed successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to apply database migrations");
        throw;
    }
}

app.Run();

public partial class Program { }
