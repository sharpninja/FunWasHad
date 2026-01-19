using FWH.Common.Location;
using FWH.Common.Location.Configuration;
using FWH.Common.Location.Extensions;
using FWH.Location.Api.Data;
using FWH.Common.Chat.Services;

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

// Add PostgreSQL with Aspire
builder.AddNpgsqlDbContext<LocationDbContext>("funwashad");

builder.Services.AddControllers();

// Add Swagger/OpenAPI for Debug builds only
#if DEBUG
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "FunWasHad Location API",
        Version = "v1",
        Description = "REST API for location tracking and business discovery. Implements TR-API-005: Location API Endpoints."
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

// Apply database migrations on startup
await ApplyDatabaseMigrationsAsync(app);

// Map Aspire default endpoints (health checks, metrics)
app.MapDefaultEndpoints();

// Enable Swagger UI for Debug builds only
#if DEBUG
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "FunWasHad Location API v1");
    options.RoutePrefix = "swagger";
    options.DisplayRequestDuration();
});
#endif

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

app.Run();

public partial class Program { }
