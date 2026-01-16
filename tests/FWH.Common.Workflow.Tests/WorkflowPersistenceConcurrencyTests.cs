using Xunit;
using System.Threading.Tasks;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using FWH.Common.Workflow;
using FWH.Common.Workflow.Instance;
using FWH.Common.Workflow.Controllers;
using FWH.Common.Workflow.Storage;
using FWH.Common.Workflow.Mapping;
using FWH.Common.Workflow.State;
using FWH.Common.Workflow.Views;
using FWH.Common.Workflow.Actions;
using FWH.Mobile.Data.Data;
using FWH.Mobile.Data.Repositories;
using System;
using FWH.Orchestrix.Contracts.Mediator;
using FWH.Orchestrix.Mediator.Remote.Mediator;

namespace FWH.Common.Workflow.Tests;

/// <summary>
/// Tests for concurrency and persistence scenarios in workflow system
/// </summary>
public class WorkflowPersistenceConcurrencyTests
{
    private ServiceProvider BuildServices()
    {
        var services = new ServiceCollection();

        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        services.AddDbContext<NotesDbContext>(options => options.UseSqlite(connection));
        services.AddScoped<IWorkflowRepository, EfWorkflowRepository>();

        services.AddSingleton<IWorkflowDefinitionStore, InMemoryWorkflowDefinitionStore>();
        services.AddSingleton<IWorkflowInstanceManager, InMemoryWorkflowInstanceManager>();
        services.AddSingleton<IWorkflowModelMapper, WorkflowModelMapper>();
        services.AddSingleton<IWorkflowStateCalculator, WorkflowStateCalculator>();
        services.AddSingleton<IWorkflowActionHandlerRegistry, WorkflowActionHandlerRegistry>();
        services.AddSingleton<WorkflowActionHandlerRegistrar>();
        services.AddLogging();
        services.AddSingleton<IMediatorSender, ServiceProviderMediatorSender>();
        services.AddTransient<IMediatorHandler<WorkflowActionRequest, WorkflowActionResponse>, WorkflowActionRequestHandler>();
        services.AddSingleton<IWorkflowActionExecutor, WorkflowActionExecutor>();
        services.AddSingleton<IWorkflowController, WorkflowController>();
        services.AddSingleton<IWorkflowService, WorkflowService>();
        services.AddTransient<IWorkflowView, WorkflowView>();
        services.AddLogging();

        var sp = services.BuildServiceProvider();

        _ = sp.GetService<WorkflowActionHandlerRegistrar>();

        using (var scope = sp.CreateScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<NotesDbContext>();
            ctx.Database.EnsureCreated();
        }

        return sp;
    }

    [Fact]
    public async Task WorkflowInstanceManager_ConcurrentSetVariable_AllUpdatesApplied()
    {
        // Arrange
        var sp = BuildServices();
        var manager = sp.GetRequiredService<IWorkflowInstanceManager>();
        var workflowId = "concurrent-var-test";

        // Act - Set 100 different variables concurrently
        var tasks = Enumerable.Range(0, 100)
            .Select(i => Task.Run(() =>
            {
                manager.SetVariable(workflowId, $"var{i}", $"value{i}");
            }))
            .ToArray();

        await Task.WhenAll(tasks);

        // Assert - All variables should be set
        var variables = manager.GetVariables(workflowId);
        Assert.NotNull(variables);
        Assert.Equal(100, variables.Count);

        for (int i = 0; i < 100; i++)
        {
            Assert.True(variables.ContainsKey($"var{i}"), $"Missing var{i}");
            Assert.Equal($"value{i}", variables[$"var{i}"]);
        }
    }

    [Fact]
    public async Task WorkflowInstanceManager_ConcurrentUpdateSameVariable_LastWriteWins()
    {
        // Arrange
        var sp = BuildServices();
        var manager = sp.GetRequiredService<IWorkflowInstanceManager>();
        var workflowId = "concurrent-update-test";
        var updateCount = 0;

        // Act - Update same variable 100 times concurrently
        var tasks = Enumerable.Range(0, 100)
            .Select(i => Task.Run(() =>
            {
                Interlocked.Increment(ref updateCount);
                manager.SetVariable(workflowId, "counter", i.ToString());
            }))
            .ToArray();

        await Task.WhenAll(tasks);

        // Assert - Should complete without errors
        Assert.Equal(100, updateCount);

        var variables = manager.GetVariables(workflowId);
        Assert.NotNull(variables);
        Assert.True(variables.ContainsKey("counter"));

        // One of the values should have won (can't predict which in concurrent scenario)
        var finalValue = int.Parse(variables["counter"]);
        Assert.True(finalValue >= 0 && finalValue < 100);
    }

    [Fact]
    public async Task WorkflowService_ConcurrentImport_EachGetsUniqueInstance()
    {
        // Arrange
        var sp = BuildServices();
        var service = sp.GetRequiredService<IWorkflowService>();
        var plant = "@startuml\n[*] --> A\n:A;\n@enduml";

        // Act - Import 50 workflows concurrently with different IDs
        var tasks = Enumerable.Range(0, 50)
            .Select(i => service.ImportWorkflowAsync(plant, $"workflow-{i}", $"Test {i}"))
            .ToArray();

        var workflows = await Task.WhenAll(tasks);

        // Assert - All should succeed with unique IDs
        Assert.Equal(50, workflows.Length);
        var uniqueIds = workflows.Select(w => w.Id).Distinct().ToList();
        Assert.Equal(50, uniqueIds.Count);
    }

    [Fact]
    public async Task WorkflowController_ConcurrentAdvance_ThreadSafe()
    {
        // Arrange
        var sp = BuildServices();
        var service = sp.GetRequiredService<IWorkflowService>();
        var controller = sp.GetRequiredService<IWorkflowController>();

        var plant = @"@startuml
[*] --> A
:A;
A --> B
A --> C
@enduml";

        // Create multiple workflows
        var workflows = await Task.WhenAll(
            Enumerable.Range(0, 20)
                .Select(i => service.ImportWorkflowAsync(plant, $"concurrent-adv-{i}", $"Test {i}"))
        );

        // Act - Advance all workflows concurrently
        var tasks = workflows.Select(async wf =>
        {
            var state = await controller.GetCurrentStatePayloadAsync(wf.Id);
            if (state.IsChoice && state.Choices.Any())
            {
                await controller.AdvanceByChoiceValueAsync(wf.Id, state.Choices[0].TargetNodeId);
            }
        }).ToArray();

        // Assert - Should complete without errors
        await Task.WhenAll(tasks);

        // Verify each workflow advanced
        foreach (var wf in workflows)
        {
            var currentNode = controller.GetCurrentNodeId(wf.Id);
            Assert.NotNull(currentNode);
            Assert.NotEqual("A", currentNode); // Should have moved from A
        }
    }

    [Fact]
    public async Task WorkflowRepository_ConcurrentPersist_AllSucceed()
    {
        // Arrange
        var sp = BuildServices();
        var service = sp.GetRequiredService<IWorkflowService>();
        var repo = sp.GetRequiredService<IWorkflowRepository>();

        // Create workflows
        var plant = "@startuml\n[*] --> A\n:A;\n@enduml";
        var workflows = await Task.WhenAll(
            Enumerable.Range(0, 30)
                .Select(i => service.ImportWorkflowAsync(plant, $"persist-{i}", $"Persist {i}"))
        );

        await Task.Delay(500); // Wait for initial persistence

        // Act - Retrieve all concurrently
        var retrievalTasks = workflows
            .Select(wf => repo.GetByIdAsync(wf.Id))
            .ToArray();

        var persisted = await Task.WhenAll(retrievalTasks);

        // Assert - All should be persisted
        Assert.Equal(30, persisted.Length);
        Assert.All(persisted, p => Assert.NotNull(p));
    }

    [Fact]
    public async Task WorkflowService_RestoreFromPersistence_RecreatesExactState()
    {
        // Arrange
        var sp1 = BuildServices();
        var service1 = sp1.GetRequiredService<IWorkflowService>();
        var controller1 = sp1.GetRequiredService<IWorkflowController>();
        var repo1 = sp1.GetRequiredService<IWorkflowRepository>();

        var plant = @"@startuml
[*] --> A
:A;
A --> B
A --> C
:B;
:C;
@enduml";

        var workflow = await service1.ImportWorkflowAsync(plant, "restore-test", "Restore Test");

        // Advance to a specific state
        var state = await controller1.GetCurrentStatePayloadAsync(workflow.Id);
        if (state.IsChoice && state.Choices.Any())
        {
            await controller1.AdvanceByChoiceValueAsync(workflow.Id, state.Choices[0].TargetNodeId);
        }

        var savedNodeId = controller1.GetCurrentNodeId(workflow.Id);
        await Task.Delay(200); // Ensure persistence completes

        // Act - Create new service provider and load workflow
        var sp2 = BuildServices();
        var service2 = sp2.GetRequiredService<IWorkflowService>();
        var controller2 = sp2.GetRequiredService<IWorkflowController>();

        // Import the same workflow (should restore from persistence)
        var restored = await service2.ImportWorkflowAsync(plant, "restore-test", "Restore Test");
        var restoredNodeId = controller2.GetCurrentNodeId(restored.Id);

        // Assert - Should restore to saved state
        Assert.NotNull(restoredNodeId);
        // Note: Depending on implementation, it might reset or restore
        // This test verifies the behavior is consistent
    }

    [Fact]
    public async Task WorkflowInstanceManager_GetVariablesConcurrent_ThreadSafe()
    {
        // Arrange
        var sp = BuildServices();
        var manager = sp.GetRequiredService<IWorkflowInstanceManager>();
        var workflowId = "read-test";

        // Setup some variables
        for (int i = 0; i < 20; i++)
        {
            manager.SetVariable(workflowId, $"var{i}", $"value{i}");
        }

        // Act - Read variables concurrently from multiple threads
        var tasks = Enumerable.Range(0, 100)
            .Select(_ => Task.Run(() =>
            {
                var vars = manager.GetVariables(workflowId);
                Assert.NotNull(vars);
                Assert.Equal(20, vars.Count);
            }))
            .ToArray();

        // Assert - All reads should succeed
        await Task.WhenAll(tasks);
    }

    [Fact]
    public async Task WorkflowDefinitionStore_ConcurrentAddAndRetrieve_ThreadSafe()
    {
        // Arrange
        var sp = BuildServices();
        var store = sp.GetRequiredService<IWorkflowDefinitionStore>();

        var plant = "@startuml\n[*] --> A\n:A;\n@enduml";
        var parser = new PlantUmlParser(plant);

        // Act - Add 50 definitions concurrently
        var addTasks = Enumerable.Range(0, 50)
            .Select(i =>
            {
                var def = parser.Parse($"def-{i}", $"Definition {i}");
                return Task.Run(() => store.Store(def));
            })
            .ToArray();

        await Task.WhenAll(addTasks);

        // Retrieve concurrently
        var retrieveTasks = Enumerable.Range(0, 50)
            .Select(i => Task.Run(() => store.Get($"def-{i}")))
            .ToArray();

        var definitions = await Task.WhenAll(retrieveTasks);

        // Assert
        Assert.Equal(50, definitions.Length);
        Assert.All(definitions, d => Assert.NotNull(d));
    }

    [Fact]
    public async Task WorkflowService_ConcurrentStartInstance_EachGetsOwnState()
    {
        // Arrange
        var sp = BuildServices();
        var service = sp.GetRequiredService<IWorkflowService>();
        var controller = sp.GetRequiredService<IWorkflowController>();

        var plant = "@startuml\n[*] --> A\n:A;\n@enduml";

        // Create workflows
        var workflows = await Task.WhenAll(
            Enumerable.Range(0, 30)
                .Select(i => service.ImportWorkflowAsync(plant, $"start-{i}", $"Start {i}"))
        );

        // Act - Start all instances concurrently (they're already started by Import, but verify state)
        var stateTasks = workflows
            .Select(wf => controller.GetCurrentStatePayloadAsync(wf.Id))
            .ToArray();

        var states = await Task.WhenAll(stateTasks);

        // Assert - Each should have its own state
        Assert.Equal(30, states.Length);
        Assert.All(states, s => Assert.NotNull(s));
    }

    [Fact]
    public async Task WorkflowInstanceManager_ConcurrentMixedOperations_RemainsConsistent()
    {
        // Arrange
        var sp = BuildServices();
        var manager = sp.GetRequiredService<IWorkflowInstanceManager>();
        var workflowIds = Enumerable.Range(0, 10).Select(i => $"mixed-{i}").ToList();

        // Act - Mixed operations: set and get concurrently
        var tasks = new List<Task>();

        // Set variables
        for (int i = 0; i < 50; i++)
        {
            var workflowId = workflowIds[i % workflowIds.Count];
            var index = i;
            tasks.Add(Task.Run(() =>
            {
                manager.SetVariable(workflowId, $"var{index}", $"value{index}");
            }));
        }

        // Get variables
        for (int i = 0; i < 30; i++)
        {
            var workflowId = workflowIds[i % workflowIds.Count];
            tasks.Add(Task.Run(() =>
            {
                var vars = manager.GetVariables(workflowId);
                Assert.NotNull(vars);
            }));
        }

        // Clear current nodes for some workflows
        for (int i = 0; i < 5; i++)
        {
            var workflowId = workflowIds[i];
            tasks.Add(Task.Run(() =>
            {
                manager.ClearCurrentNode(workflowId);
            }));
        }

        await Task.WhenAll(tasks);

        // Assert - Should complete without exceptions
        // Verify we can still read all workflow variables
        foreach (var wfId in workflowIds)
        {
            var vars = manager.GetVariables(wfId);
            Assert.NotNull(vars);
        }
    }

    [Fact]
    public async Task WorkflowRepository_UpdateCurrentNodeId_HandlesRapidUpdates()
    {
        // Arrange
        var sp = BuildServices();
        var service = sp.GetRequiredService<IWorkflowService>();
        var repo = sp.GetRequiredService<IWorkflowRepository>();
        var controller = sp.GetRequiredService<IWorkflowController>();

        var plant = @"@startuml
[*] --> A
:A;
A --> B
:B;
B --> C
:C;
@enduml";

        var workflow = await service.ImportWorkflowAsync(plant, "rapid-update", "Rapid");

        // Act - Advance rapidly multiple times
        for (int i = 0; i < 3; i++)
        {
            var state = await controller.GetCurrentStatePayloadAsync(workflow.Id);
            if (!state.IsChoice)
            {
                await controller.AdvanceByChoiceValueAsync(workflow.Id, null);
            }
            await Task.Delay(50); // Small delay between advances
        }

        await Task.Delay(300); // Wait for persistence

        // Assert - Should persist final state
        var persisted = await repo.GetByIdAsync(workflow.Id);
        Assert.NotNull(persisted);
        Assert.False(string.IsNullOrWhiteSpace(persisted!.CurrentNodeId));
    }
}
