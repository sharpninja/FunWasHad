using FWH.Mobile.Data.Models;
using FWH.Mobile.Data.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FWH.Mobile.Data.Tests;

public class WorkflowCrudTests : DataTestBase
{
    /// <summary>
    /// Tests that UpdateAsync replaces all collections (Nodes, Transitions, StartPoints) rather than merging them.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The IWorkflowRepository.UpdateAsync method's behavior when updating a workflow with different collections than the original.</para>
    /// <para><strong>Data involved:</strong> A workflow "u1" is created with Name="Before", node n1, transition n1→n2, and start point n1. Then it's updated with Name="After", node n2, transition n2→n3, and start point n2. The updated workflow has completely different nodes, transitions, and start points.</para>
    /// <para><strong>Why the data matters:</strong> Workflow updates may completely change the workflow structure (e.g., redesigning the flow). The repository must replace collections rather than merge them, otherwise old nodes/transitions would remain alongside new ones, creating invalid workflows. This tests that UpdateAsync performs a full replacement, ensuring the persisted workflow matches the updated definition exactly.</para>
    /// <para><strong>Expected outcome:</strong> After UpdateAsync, the workflow should have Name="After", exactly 1 node (n2), exactly 1 transition (n2→n3), and exactly 1 start point (n2). The old node n1, transition n1→n2, and start point n1 should be removed.</para>
    /// <para><strong>Reason for expectation:</strong> UpdateAsync should replace the entire workflow definition, not merge changes. This means removing old nodes/transitions/start points and adding new ones. The exact counts (1 node, 1 transition, 1 start point) and the specific IDs (n2) confirm that the old collections were completely replaced, not merged. This ensures workflow updates result in clean, valid workflow structures.</para>
    /// </remarks>
    [Fact]
    public async Task UpdateWorkflowReplacesCollections()
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

        await repo.CreateAsync(def).ConfigureAwait(true);

        // Create a fresh instance to represent the updated state (avoid modifying tracked entity)
        var updatedDef = new WorkflowDefinitionEntity
        {
            Id = "u1",
            Name = "After"
        };
        updatedDef.Nodes.Add(new NodeEntity { NodeId = "n2", Text = "Two" });
        updatedDef.Transitions.Add(new TransitionEntity { FromNodeId = "n2", ToNodeId = "n3" });
        updatedDef.StartPoints.Add(new StartPointEntity { NodeId = "n2" });

        var updated = await repo.UpdateAsync(updatedDef).ConfigureAwait(true);
        Assert.Equal("After", updated.Name);
        Assert.Single(updated.Nodes);
        Assert.Equal("n2", updated.Nodes[0].NodeId);
        Assert.Single(updated.Transitions);
        Assert.Single(updated.StartPoints);
        Assert.Equal("n2", updated.StartPoints[0].NodeId);
    }

    /// <summary>
    /// Tests that DeleteAsync removes a workflow from the repository and subsequent queries return null.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The IWorkflowRepository.DeleteAsync method's ability to permanently remove a workflow definition from the database.</para>
    /// <para><strong>Data involved:</strong> A workflow "d1" with Name="ToDelete" is created, then deleted. The test verifies the workflow exists before deletion and is null after deletion.</para>
    /// <para><strong>Why the data matters:</strong> Workflow deletion is necessary for cleanup, removing obsolete workflows, or allowing users to delete their workflows. The repository must completely remove the workflow and all its related data (nodes, transitions, start points) to prevent orphaned records and ensure GetByIdAsync correctly reflects the deletion.</para>
    /// <para><strong>Expected outcome:</strong> Before deletion, GetByIdAsync("d1") should return a non-null workflow. After DeleteAsync("d1") returns true, GetByIdAsync("d1") should return null.</para>
    /// <para><strong>Reason for expectation:</strong> DeleteAsync should remove the workflow entity and all related entities (via cascade delete or explicit deletion). The true return value indicates successful deletion. After deletion, querying for the workflow should return null, confirming it no longer exists in the database. This ensures deletion is permanent and complete.</para>
    /// </remarks>
    [Fact]
    public async Task DeleteWorkflowRemovesIt()
    {
        var repo = ServiceProvider.GetRequiredService<IWorkflowRepository>();

        var def = new WorkflowDefinitionEntity { Id = "d1", Name = "ToDelete" };
        await repo.CreateAsync(def).ConfigureAwait(true);

        var got = await repo.GetByIdAsync("d1").ConfigureAwait(true);
        Assert.NotNull(got);

        var deleted = await repo.DeleteAsync("d1").ConfigureAwait(true);
        Assert.True(deleted);

        var after = await repo.GetByIdAsync("d1").ConfigureAwait(true);
        Assert.Null(after);
    }
}
