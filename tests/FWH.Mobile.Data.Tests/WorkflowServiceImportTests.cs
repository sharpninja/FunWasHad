using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using FWH.Common.Workflow;
using FWH.Mobile.Data.Repositories;
using System;

namespace FWH.Mobile.Data.Tests;

public class WorkflowServiceImportTests : DataTestBase
{
    /// <summary>
    /// Tests that ImportWorkflowAsync correctly parses a basic PlantUML workflow and persists all components (nodes, transitions, start points) to the database.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The IWorkflowService.ImportWorkflowAsync method's ability to parse PlantUML, create a workflow definition, and persist it to the repository with all components.</para>
    /// <para><strong>Data involved:</strong> A simple PlantUML workflow with a start state [*] transitioning to "Start" node, and "Start" transitioning to "End" node. The workflow is imported with Id="wf_basic" and Name="Basic".</para>
    /// <para><strong>Why the data matters:</strong> ImportWorkflowAsync is the primary method for creating workflows from PlantUML definitions. It must correctly parse the PlantUML syntax, extract all workflow components (nodes, transitions, start points), and persist them to the database. This test validates the complete import-to-persistence flow works correctly for a basic workflow structure.</para>
    /// <para><strong>Expected outcome:</strong> The returned workflow definition should have Id="wf_basic", Name="Basic", 2 nodes (Start and End), 1 start point (pointing to Start), and transitions matching the PlantUML. Querying the repository should return the same workflow with all components persisted correctly.</para>
    /// <para><strong>Reason for expectation:</strong> ImportWorkflowAsync should parse the PlantUML, create a WorkflowDefinition with all components, persist it via the repository, and return the definition. The repository should store all nodes, transitions, and start points with correct relationships. The exact counts and content matches confirm that parsing and persistence work correctly together, ensuring workflows can be imported and later retrieved with full fidelity.</para>
    /// </remarks>
    [Fact]
    public async Task ImportWorkflow_Basic_PersistsNodesAndStartPoints()
    {
        var service = ServiceProvider.GetRequiredService<IWorkflowService>();

        var plant = @"
            @startuml
            [*] --> :Start;
            :Start --> :End;
            @enduml
        ";

        var def = await service.ImportWorkflowAsync(plant, id: "wf_basic", name: "Basic");

        Assert.Equal("wf_basic", def.Id);
        Assert.Equal("Basic", def.Name);
        Assert.Equal(2, def.Nodes.Count);
        Assert.Single(def.StartPoints);

        var repo = ServiceProvider.GetRequiredService<IWorkflowRepository>();
        var persisted = await repo.GetByIdAsync("wf_basic");
        Assert.NotNull(persisted);
        Assert.Equal("wf_basic", persisted!.Id);
        Assert.Equal("Basic", persisted.Name);
        Assert.Equal(2, persisted.Nodes.Count);
        Assert.Single(persisted.StartPoints);
        Assert.Contains(persisted.Nodes, n => n.Text == "Start");
        Assert.Contains(persisted.Nodes, n => n.Text == "End");
        Assert.Equal(def.Transitions.Count, persisted.Transitions.Count);

        // Verify transition IDs and mapping
        foreach (var t in def.Transitions)
        {
            Assert.False(string.IsNullOrWhiteSpace(t.Id));
            Assert.NotNull(persisted.Transitions.FirstOrDefault(pt => pt.FromNodeId == t.FromNodeId && pt.ToNodeId == t.ToNodeId));
        }
    }

    [Fact]
    public async Task ImportWorkflow_IfBranch_PersistsTransitionWithConditionAndIds()
    {
        var service = ServiceProvider.GetRequiredService<IWorkflowService>();

        var plant = @"
            @startuml
            :Start;
            if (isValid) then (yes)
            :Valid;
            else if (isMaybe) then (maybe)
            :Maybe;
            else (no)
            :Invalid;
            endif
            :Valid --> :End;
            :Maybe --> :End;
            :Invalid --> :End;
            @enduml
        ";

        var def = await service.ImportWorkflowAsync(plant, id: "wf_if", name: "IfTest");

        Assert.Equal("wf_if", def.Id);

        var repo = ServiceProvider.GetRequiredService<IWorkflowRepository>();
        var persisted = await repo.GetByIdAsync("wf_if");
        Assert.NotNull(persisted);

        // Ensure transitions include expected conditions
        var conditions = def.Transitions.Select(t => t.Condition).Where(c => c != null).ToList();
        Assert.Contains("isValid", conditions);
        Assert.Contains("isMaybe", conditions);

        // Ensure persisted transitions contain same mapping (condition may be stored on the transition entity)
        Assert.Contains(persisted.Transitions, t => t.Condition == "isValid");
        Assert.Contains(persisted.Transitions, t => t.Condition == "isMaybe");

        // Verify that each parsed transition has a matching persisted transition
        foreach (var t in def.Transitions)
        {
            var match = persisted.Transitions.FirstOrDefault(pt => pt.FromNodeId == t.FromNodeId && pt.ToNodeId == t.ToNodeId && (pt.Condition ?? string.Empty) == (t.Condition ?? string.Empty));
            Assert.NotNull(match);
        }

        // Verify synthetic 'join' node exists in parsed and persisted nodes
        Assert.Contains(def.Nodes, n => string.Equals(n.Label, "join", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(persisted.Nodes, n => string.Equals(n.Text, "join", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ImportWorkflow_Loop_PersistsLoopTransitions()
    {
        var service = ServiceProvider.GetRequiredService<IWorkflowService>();

        var plant = @"
            @startuml
            :Start;
            repeat
            :Work;
            repeat while (notDone)
            :End;
            @enduml
        ";

        var def = await service.ImportWorkflowAsync(plant, id: "wf_loop", name: "LoopTest");

        Assert.Equal("wf_loop", def.Id);
        Assert.True(def.Transitions.Count >= 2);

        var repo = ServiceProvider.GetRequiredService<IWorkflowRepository>();
        var persisted = await repo.GetByIdAsync("wf_loop");
        Assert.NotNull(persisted);

        // Ensure there's a transition with condition 'notDone'
        Assert.Contains(def.Transitions, t => t.Condition == "notDone");
        Assert.Contains(persisted.Transitions, t => t.Condition == "notDone");

        // Verify synthetic loop labels exist
        Assert.Contains(def.Nodes, n => string.Equals(n.Label, "loop_entry", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(def.Nodes, n => string.Equals(n.Label, "after_loop", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(persisted.Nodes, n => string.Equals(n.Text, "loop_entry", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(persisted.Nodes, n => string.Equals(n.Text, "after_loop", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ImportWorkflow_NullInput_ThrowsArgumentNullException()
    {
        var service = ServiceProvider.GetRequiredService<IWorkflowService>();
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await service.ImportWorkflowAsync(null!));
    }

    [Fact]
    public async Task ImportWorkflow_UnclosedIf_ProducesJoinNode()
    {
        var service = ServiceProvider.GetRequiredService<IWorkflowService>();

        var plant = @"
            @startuml
            :Start;
            if (cond) then (y)
            :Then;
            @enduml
        ";

        var def = await service.ImportWorkflowAsync(plant, id: "wf_unclosed_if", name: "UnclosedIf");
        Assert.Contains(def.Nodes, n => string.Equals(n.Label, "join", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ImportWorkflow_RepeatWithoutWhile_ProducesAfterLoop()
    {
        var service = ServiceProvider.GetRequiredService<IWorkflowService>();

        var plant = @"
            @startuml
            :Start;
            repeat
            :Work;
            @enduml
        ";

        var def = await service.ImportWorkflowAsync(plant, id: "wf_repeat", name: "RepeatTest");
        Assert.Contains(def.Nodes, n => string.Equals(n.Label, "after_loop", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ImportWorkflow_GarbageInput_DoesNotThrowAndParsesValidLines()
    {
        var service = ServiceProvider.GetRequiredService<IWorkflowService>();

        var plant = @"
            @startuml
            This is not UML
            :Start;
            :Start --> :End;
            Some random text
            @enduml
        ";

        var def = await service.ImportWorkflowAsync(plant, id: "wf_garbage", name: "Garbage");
        Assert.Contains(def.Nodes, n => string.Equals(n.Label, "Start", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(def.Transitions, t => t.ToNodeId != null);
    }
}
