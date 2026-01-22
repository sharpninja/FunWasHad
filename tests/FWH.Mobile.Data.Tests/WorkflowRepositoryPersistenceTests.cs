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
    /// <summary>
    /// Tests that UpdateCurrentNodeIdAsync correctly persists the current node ID to the database.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The IWorkflowRepository.UpdateCurrentNodeIdAsync method's ability to update and persist the CurrentNodeId property of a workflow definition.</para>
    /// <para><strong>Data involved:</strong> A WorkflowDefinitionEntity with Id="wfp", Name="p", and CurrentNodeId="A" is created. Then UpdateCurrentNodeIdAsync is called to change CurrentNodeId to "B". The test uses an in-memory SQLite database.</para>
    /// <para><strong>Why the data matters:</strong> The current node ID tracks workflow execution state - which node the workflow is currently at. This state must be persisted so workflows can resume after application restarts. UpdateCurrentNodeIdAsync provides an efficient way to update just the current node without loading the entire workflow definition. This is important for performance when workflows advance frequently.</para>
    /// <para><strong>Expected outcome:</strong> UpdateCurrentNodeIdAsync should return true (indicating success), and querying the persisted workflow should show CurrentNodeId="B" (updated from "A").</para>
    /// <para><strong>Reason for expectation:</strong> The method should update the CurrentNodeId column in the database for the specified workflow. The true return value confirms the update succeeded. Querying the workflow after the update should reflect the new CurrentNodeId, confirming the change was persisted. This validates that workflow state updates are correctly saved to the database.</para>
    /// </remarks>
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
