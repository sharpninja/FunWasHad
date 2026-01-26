using FWH.Mobile.Data.Data;
using FWH.Mobile.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FWH.Mobile.Data.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMobileData(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<NotesDbContext>(options => options.UseSqlite(connectionString));
        services.AddScoped<INoteRepository, EfNoteRepository>();
        services.AddScoped<IWorkflowRepository, EfWorkflowRepository>();

        // Ensure DB created
        using var provider = services.BuildServiceProvider();
        var ctx = provider.GetRequiredService<NotesDbContext>();
        ctx.Database.EnsureCreated();
        return services;
    }
}
