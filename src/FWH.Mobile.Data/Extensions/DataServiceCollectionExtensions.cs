using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using FWH.Mobile.Data.Data;
using FWH.Mobile.Data.Repositories;

namespace FWH.Mobile.Data.Extensions;

/// <summary>
/// Extension methods for registering data services with dependency injection.
/// Single Responsibility: Configure DI for data and repository components.
/// </summary>
public static class DataServiceCollectionExtensions
{
    /// <summary>
    /// Adds all data services to the service collection with in-memory SQLite database.
    /// Includes DbContext and repositories.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="connectionString">The SQLite connection string. Defaults to in-memory database.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddDataServices(
        this IServiceCollection services,
        string connectionString = "DataSource=:memory:")
    {
        // Register DbContext with SQLite
        services.AddDbContext<NotesDbContext>(options => 
            options.UseSqlite(connectionString));

        // Register repositories
        services.AddScoped<INoteRepository, EfNoteRepository>();
        services.AddScoped<IWorkflowRepository, EfWorkflowRepository>();

        return services;
    }

    /// <summary>
    /// Adds DbContext with the specified database provider configuration.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="optionsAction">Action to configure DbContext options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddNotesDbContext(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> optionsAction)
    {
        services.AddDbContext<NotesDbContext>(optionsAction);
        return services;
    }

    /// <summary>
    /// Adds repository implementations to the service collection.
    /// Requires DbContext to be registered first.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<INoteRepository, EfNoteRepository>();
        services.AddScoped<IWorkflowRepository, EfWorkflowRepository>();
        services.AddScoped<IConfigurationRepository, EfConfigurationRepository>();
        
        return services;
    }

    /// <summary>
    /// Adds data services with a custom DbContext configuration.
    /// Useful for testing or custom database setups.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="optionsAction">Action to configure DbContext options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddDataServicesWithCustomDb(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> optionsAction)
    {
        services.AddNotesDbContext(optionsAction);
        services.AddRepositories();
        
        return services;
    }
}
