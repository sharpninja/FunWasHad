using System.Threading.Tasks;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using FWH.Mobile.Data.Repositories;
using FWH.Mobile.Data.Data;

namespace FWH.Mobile.Data.Tests;

public class WorkflowRepositoryPersistenceTests : DataTestBase
{
    [Fact]
    public async Task UpdateCurrentNodeIdAsync_PersistsCurrentNode()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var services = new ServiceCollection();
        services.AddDbContext<NotesDbContext>(options => options.UseSqlite(connection));
        services.AddScoped<IWorkflowRepository, EfWorkflowRepository>();

        var sp = services.BuildServiceProvider();
        using (var scope = sp.CreateScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<NotesDbContext>();
            ctx.Database.EnsureCreated();
        }

        var repo = sp.GetRequiredService<IWorkflowRepository>();

        var def = new FWH.Mobile.Data.Models.WorkflowDefinitionEntity { Id = "wfp", Name = "p", CurrentNodeId = "A" };
        await repo.CreateAsync(def);

        var updated = await repo.UpdateCurrentNodeIdAsync("wfp", "B");
        Assert.True(updated);

        var persisted = await repo.GetByIdAsync("wfp");
        Assert.NotNull(persisted);
        Assert.Equal("B", persisted!.CurrentNodeId);
    }
}
