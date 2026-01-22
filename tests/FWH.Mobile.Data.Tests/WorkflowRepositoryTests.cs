using System.Threading.Tasks;
using Xunit;
using FWH.Mobile.Data.Models;
using FWH.Mobile.Data.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace FWH.Mobile.Data.Tests;

public class WorkflowRepositoryTests : DataTestBase
{
    /// <summary>
    /// Tests that a workflow definition can be created and subsequently retrieved from the repository with all its components.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The IWorkflowRepository's CreateAsync and GetByIdAsync methods for persisting and retrieving complete workflow definitions.</para>
    /// <para><strong>Data involved:</strong> A WorkflowDefinitionEntity with Id="wf1", Name="Test Workflow", containing two nodes (n1="Start", n2="End"), one transition (n1 → n2, no condition), and one start point (n1). This represents a minimal but complete workflow definition with all required components.</para>
    /// <para><strong>Why the data matters:</strong> Workflow definitions are complex entities with multiple related components (nodes, transitions, start points). This test validates that the repository correctly persists all components and their relationships. The test data includes all workflow elements to ensure complete round-trip persistence works correctly.</para>
    /// <para><strong>Expected outcome:</strong> After creating the workflow, GetByIdAsync("wf1") should return a workflow with matching Id, Name="Test Workflow", exactly 2 nodes, exactly 1 transition, exactly 1 start point, and the start point should have NodeId="n1".</para>
    /// <para><strong>Reason for expectation:</strong> The repository should persist all workflow components (nodes, transitions, start points) and their relationships. When retrieving, all components should be loaded with the same values that were saved. The exact counts (2 nodes, 1 transition, 1 start point) confirm that all components were persisted and retrieved correctly, validating the Entity Framework relationship mappings.</para>
    /// </remarks>
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

    /// <summary>
    /// Tests that GetAllAsync returns all workflows including newly created ones.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The IWorkflowRepository's GetAllAsync method's ability to return all persisted workflow definitions.</para>
    /// <para><strong>Data involved:</strong> A WorkflowDefinitionEntity with Id="wf2", Name="Another Workflow", containing one node (NodeId="a", Text="A"). This workflow is created and then GetAllAsync is called to verify it appears in the results.</para>
    /// <para><strong>Why the data matters:</strong> The GetAllAsync method is used to list all available workflows (e.g., for workflow selection UI). This test validates that newly created workflows are immediately available in the list, ensuring the repository correctly persists and retrieves workflows. The test uses a different workflow ID ("wf2") to avoid conflicts with other tests.</para>
    /// <para><strong>Expected outcome:</strong> After creating the workflow, GetAllAsync should return a collection containing at least one workflow with Id="wf2".</para>
    /// <para><strong>Reason for expectation:</strong> The repository should persist workflows immediately upon creation, and GetAllAsync should return all persisted workflows. The presence of "wf2" in the results confirms that the workflow was successfully persisted and is retrievable through the GetAllAsync method. This validates both the create and list operations work correctly together.</para>
    /// </remarks>
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
