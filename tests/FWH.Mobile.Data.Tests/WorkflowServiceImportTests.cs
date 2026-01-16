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
