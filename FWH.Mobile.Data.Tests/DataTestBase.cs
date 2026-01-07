using System;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using FWH.Mobile.Data.Data;
using FWH.Mobile.Data.Repositories;
using FWH.Common.Workflow.Extensions;

namespace FWH.Mobile.Data.Tests;

public abstract class DataTestBase : IDisposable
{
    protected readonly IServiceProvider ServiceProvider;
    protected readonly SqliteConnection Connection;

    protected DataTestBase()
    {
        Connection = new SqliteConnection("DataSource=:memory:");
        Connection.Open();

        var services = new ServiceCollection();

        services.AddDbContext<NotesDbContext>(options => options.UseSqlite(Connection));
        services.AddScoped<INoteRepository, EfNoteRepository>();
        services.AddScoped<IWorkflowRepository, EfWorkflowRepository>();

        // Register workflow services using extension method
        services.AddWorkflowServices();

        services.AddLogging();

        ServiceProvider = services.BuildServiceProvider();

        // Ensure database schema is created
        using var scope = ServiceProvider.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<NotesDbContext>();
        ctx.Database.EnsureCreated();
    }

    public void Dispose()
    {
        if (ServiceProvider is IDisposable d) d.Dispose();
        try
        {
            Connection.Close();
            Connection.Dispose();
        }
        catch { }
    }
}
