using FWH.MarketingApi.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to listen on all interfaces (0.0.0.0) in Development
// This allows Android devices and emulators to connect via host IP
if (builder.Environment.IsDevelopment())
{
    builder.WebHost.ConfigureKestrel(options =>
    {
        // Listen on all interfaces for HTTP (port configured by Aspire: 4750)
        options.ListenAnyIP(4750);

        // Listen on all interfaces for HTTPS (port configured by Aspire: 4749)
        options.ListenAnyIP(4749, listenOptions =>
        {
            listenOptions.UseHttps();
        });
    });
}

// Add Aspire service defaults (telemetry, health checks, resilience)
builder.AddServiceDefaults();

// Add PostgreSQL with Aspire
builder.AddNpgsqlDbContext<MarketingDbContext>("marketing");

// Add controllers with JSON options to handle circular references
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

// Add problem details
builder.Services.AddProblemDetails();

// Add blob storage service
// For Railway staging, use local file storage with persistent volume
// For production, consider cloud storage (S3, Azure Blob, etc.)
builder.Services.AddSingleton<FWH.MarketingApi.Services.IBlobStorageService,
    FWH.MarketingApi.Services.LocalFileBlobStorageService>();

// Add Swagger/OpenAPI for Debug and Staging builds
#if DEBUG || STAGING
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "FunWasHad Marketing API",
        Version = "v1",
        Description = "REST API for business marketing data, feedback, and attachments. Implements TR-API-002 and TR-API-003."
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

var app = builder.Build();

// Apply database migrations on startup
await ApplyDatabaseMigrationsAsync(app);

// Map Aspire default endpoints (health checks, metrics)
app.MapDefaultEndpoints();

// Enable Swagger UI for Debug and Staging builds
#if DEBUG || STAGING
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "FunWasHad Marketing API v1");
    options.RoutePrefix = "swagger";
    options.DisplayRequestDuration();
});
#endif

// Configure error handling
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler();
    app.UseHsts();
}

// Add API key authentication middleware
// Note: In Development/Staging, authentication is optional (can be disabled via config)
var requireAuth = app.Configuration.GetValue<bool>("ApiSecurity:RequireAuthentication", defaultValue: true);
if (requireAuth)
{
    app.UseMiddleware<FWH.MarketingApi.Middleware.ApiKeyAuthenticationMiddleware>();
}

app.UseHttpsRedirection();

// Serve static files from uploads directory
var uploadsPath = app.Configuration["BlobStorage:LocalPath"]
    ?? Path.Combine(Directory.GetCurrentDirectory(), "uploads");
if (Directory.Exists(uploadsPath))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadsPath),
        RequestPath = "/uploads"
    });
    app.Logger.LogInformation("Static file serving enabled for uploads at /uploads");
}

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
        var connectionString = configuration.GetConnectionString("marketing");

        if (string.IsNullOrEmpty(connectionString))
        {
            logger.LogWarning("Database connection string 'marketing' not found. Skipping migrations (likely using in-memory database for testing).");
            return;
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
