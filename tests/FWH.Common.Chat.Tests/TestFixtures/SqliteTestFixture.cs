using FWH.Common.Chat.Extensions;
using FWH.Common.Workflow.Extensions;
using FWH.Mobile.Data.Data;
using FWH.Mobile.Data.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FWH.Common.Chat.Tests.TestFixtures;

public class SqliteTestFixture : IDisposable
{
    public SqliteConnection Connection { get; }
    public TestLoggerProvider LoggerProvider { get; }

    public SqliteTestFixture()
    {
        Connection = new SqliteConnection("DataSource=:memory:");
        Connection.Open();

        LoggerProvider = new TestLoggerProvider();

        // Ensure the database schema exists by creating a temporary provider
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddProvider(LoggerProvider));
        services.AddDbContext<NotesDbContext>(options => options.UseSqlite(Connection));
        services.AddScoped<IWorkflowRepository, EfWorkflowRepository>();

        // Register workflow and chat services using extension methods
        services.AddWorkflowServices();
        services.AddChatServices();

        var sp = services.BuildServiceProvider();
        using (sp as IDisposable)
        {
            using var scope = sp.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<NotesDbContext>();
            ctx.Database.EnsureCreated();
        }
    }

    /// <summary>
    /// Create a new ServiceProvider that uses the shared in-memory SQLite connection and test logger.
    /// Callers can add/override services via the configure callback.
    /// </summary>
    public IServiceProvider CreateServiceProvider(Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddProvider(LoggerProvider));
        services.AddDbContext<NotesDbContext>(options => options.UseSqlite(Connection));
        services.AddScoped<IWorkflowRepository, EfWorkflowRepository>();

        // Register workflow and chat services using extension methods
        services.AddWorkflowServices();
        services.AddChatServices();

        configure?.Invoke(services);

        return services.BuildServiceProvider();
    }

    public void Dispose()
    {
        try { Connection.Close(); } catch { }
        try { Connection.Dispose(); } catch { }
    }
}
