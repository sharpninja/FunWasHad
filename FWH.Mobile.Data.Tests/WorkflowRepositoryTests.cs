using System.Threading.Tasks;
using Xunit;
using FWH.Mobile.Data.Models;
using FWH.Mobile.Data.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace FWH.Mobile.Data.Tests;

public class WorkflowRepositoryTests : DataTestBase
{
    [Fact]
    public async Task CreateAndRetrieveWorkflow()
    {
        var repo = ServiceProvider.GetRequiredService<IWorkflowRepository>();

        var def = new WorkflowDefinitionEntity
        {
            Id = "wf1",
            Name = "Test Workflow"
        };

        def.Nodes.Add(new NodeEntity { NodeId = "n1", Text = "Start" });
        def.Nodes.Add(new NodeEntity { NodeId = "n2", Text = "End" });

        def.Transitions.Add(new TransitionEntity { FromNodeId = "n1", ToNodeId = "n2", Condition = null });

        def.StartPoints.Add(new StartPointEntity { NodeId = "n1" });

        var created = await repo.CreateAsync(def);
        Assert.Equal("wf1", created.Id);

        var got = await repo.GetByIdAsync("wf1");
        Assert.NotNull(got);
        Assert.Equal("Test Workflow", got!.Name);
        Assert.Equal(2, got.Nodes.Count);
        Assert.Single(got.Transitions);
        Assert.Single(got.StartPoints);
        Assert.Equal("n1", got.StartPoints[0].NodeId);
    }

    [Fact]
    public async Task GetAllContainsCreatedWorkflow()
    {
        var repo = ServiceProvider.GetRequiredService<IWorkflowRepository>();

        var def = new WorkflowDefinitionEntity
        {
            Id = "wf2",
            Name = "Another Workflow"
        };
        def.Nodes.Add(new NodeEntity { NodeId = "a", Text = "A" });
        await repo.CreateAsync(def);

        var all = await repo.GetAllAsync();
        Assert.Contains(all, w => w.Id == "wf2");
    }
}
