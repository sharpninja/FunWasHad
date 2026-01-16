using System.Threading.Tasks;
using Xunit;
using FWH.Mobile.Data.Models;
using FWH.Mobile.Data.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace FWH.Mobile.Data.Tests;

public class WorkflowCrudTests : DataTestBase
{
    [Fact]
    public async Task UpdateWorkflow_ReplacesCollections()
    {
        var repo = ServiceProvider.GetRequiredService<IWorkflowRepository>();

        var def = new WorkflowDefinitionEntity
        {
            Id = "u1",
            Name = "Before"
        };
        def.Nodes.Add(new NodeEntity { NodeId = "n1", Text = "One" });
        def.Transitions.Add(new TransitionEntity { FromNodeId = "n1", ToNodeId = "n2" });
        def.StartPoints.Add(new StartPointEntity { NodeId = "n1" });

        await repo.CreateAsync(def);

        // Create a fresh instance to represent the updated state (avoid modifying tracked entity)
        var updatedDef = new WorkflowDefinitionEntity
        {
            Id = "u1",
            Name = "After"
        };
        updatedDef.Nodes.Add(new NodeEntity { NodeId = "n2", Text = "Two" });
        updatedDef.Transitions.Add(new TransitionEntity { FromNodeId = "n2", ToNodeId = "n3" });
        updatedDef.StartPoints.Add(new StartPointEntity { NodeId = "n2" });

        var updated = await repo.UpdateAsync(updatedDef);
        Assert.Equal("After", updated.Name);
        Assert.Single(updated.Nodes);
        Assert.Equal("n2", updated.Nodes[0].NodeId);
        Assert.Single(updated.Transitions);
        Assert.Single(updated.StartPoints);
        Assert.Equal("n2", updated.StartPoints[0].NodeId);
    }

    [Fact]
    public async Task DeleteWorkflow_RemovesIt()
    {
        var repo = ServiceProvider.GetRequiredService<IWorkflowRepository>();

        var def = new WorkflowDefinitionEntity { Id = "d1", Name = "ToDelete" };
        await repo.CreateAsync(def);

        var got = await repo.GetByIdAsync("d1");
        Assert.NotNull(got);

        var deleted = await repo.DeleteAsync("d1");
        Assert.True(deleted);

        var after = await repo.GetByIdAsync("d1");
        Assert.Null(after);
    }
}
