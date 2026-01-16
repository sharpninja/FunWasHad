using FWH.MarketingApi.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults (telemetry, health checks, resilience)
builder.AddServiceDefaults();

// Add PostgreSQL with Aspire
builder.AddNpgsqlDbContext<MarketingDbContext>("marketing");

// Add controllers
builder.Services.AddControllers();

// Add problem details
builder.Services.AddProblemDetails();

// Add Swagger/OpenAPI for Debug builds only
#if DEBUG
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

// Enable Swagger UI for Debug builds only
#if DEBUG
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

app.UseHttpsRedirection();
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
        var connectionString = configuration.GetConnectionString("marketing");

        if (string.IsNullOrEmpty(connectionString))
        {
            logger.LogError("Database connection string 'marketing' not found");
            throw new InvalidOperationException("Database connection string 'marketing' is required");
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
