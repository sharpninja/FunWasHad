using FWH.Common.Location;
using FWH.Common.Location.Configuration;
using FWH.Common.Location.Extensions;
using FWH.Location.Api.Data;
using FWH.Common.Chat.Services;

var builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults (telemetry, health checks, resilience)
builder.AddServiceDefaults();

// Add PostgreSQL with Aspire
builder.AddNpgsqlDbContext<LocationDbContext>("funwashad");

builder.Services.AddControllers();

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

// Apply database migrations on startup
await ApplyDatabaseMigrationsAsync(app);

// Map Aspire default endpoints (health checks, metrics)
app.MapDefaultEndpoints();

// HTTPS redirection disabled for Android development
// Android emulator connects via HTTP (http://10.0.2.2:4748)
// Enable in production with proper SSL certificates
// app.UseHttpsRedirection();

app.MapControllers();

app.Run();

/// <summary>
/// Applies database migrations on application startup.
/// </summary>
static async Task ApplyDatabaseMigrationsAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        logger.LogInformation("Checking for database migrations...");
        
        // Get connection string from configuration
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var connectionString = configuration.GetConnectionString("funwashad");
        
        if (string.IsNullOrEmpty(connectionString))
        {
            logger.LogError("Database connection string 'funwashad' not found");
            throw new InvalidOperationException("Database connection string 'funwashad' is required");
        }
        
        // Create and run migration service
        var migrationLogger = scope.ServiceProvider.GetRequiredService<ILogger<DatabaseMigrationService>>();
        var migrationService = new DatabaseMigrationService(connectionString, migrationLogger);
        
        await migrationService.ApplyMigrationsAsync();
        
        logger.LogInformation("Database migrations completed successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to apply database migrations");
        throw;
    }
}

public partial class Program { }
