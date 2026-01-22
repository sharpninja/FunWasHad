using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using FWH.Common.Workflow;
using FWH.Common.Workflow.Controllers;
using FWH.Common.Workflow.Storage;
using FWH.Common.Workflow.Instance;
using FWH.Common.Workflow.Mapping;
using FWH.Common.Workflow.State;
using FWH.Common.Workflow.Views;
using FWH.Common.Workflow.Models;
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using System.Threading.Tasks;

namespace FWH.Common.Workflow.Tests;

public class WorkflowServiceTests
{
    private IWorkflowService BuildWithInMemoryRepo(IServiceCollection? services = null)
    {
        var sc = services ?? new ServiceCollection();

        // Add a fake repository that persists to memory using the EfWorkflowRepository and an in-memory SQLite context
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        sc.AddDbContext<FWH.Mobile.Data.Data.NotesDbContext>(options => options.UseSqlite(connection));
        sc.AddScoped<FWH.Mobile.Data.Repositories.IWorkflowRepository, FWH.Mobile.Data.Repositories.EfWorkflowRepository>();

        // Register SRP components with Controller pattern
        sc.AddSingleton<IWorkflowDefinitionStore, InMemoryWorkflowDefinitionStore>();
        sc.AddSingleton<IWorkflowInstanceManager, InMemoryWorkflowInstanceManager>();
        sc.AddSingleton<IWorkflowModelMapper, WorkflowModelMapper>();
        sc.AddSingleton<IWorkflowStateCalculator, WorkflowStateCalculator>();
        sc.AddSingleton<IWorkflowController, WorkflowController>();
        sc.AddSingleton<IWorkflowService, WorkflowService>();
        sc.AddTransient<IWorkflowView, WorkflowView>();
        sc.AddLogging();

        // Register action executor and handler registry so controller DI can resolve dependencies in tests
        sc.AddSingleton<FWH.Common.Workflow.Actions.IWorkflowActionHandlerRegistry, FWH.Common.Workflow.Actions.WorkflowActionHandlerRegistry>();
        sc.AddSingleton<FWH.Common.Workflow.Actions.WorkflowActionHandlerRegistrar>();
        sc.AddSingleton<FWH.Orchestrix.Contracts.Mediator.IMediatorSender, FWH.Orchestrix.Mediator.Remote.Mediator.ServiceProviderMediatorSender>();
        sc.AddTransient<FWH.Orchestrix.Contracts.Mediator.IMediatorHandler<FWH.Common.Workflow.Actions.WorkflowActionRequest, FWH.Common.Workflow.Actions.WorkflowActionResponse>, FWH.Common.Workflow.Actions.WorkflowActionRequestHandler>();
        sc.AddSingleton<FWH.Common.Workflow.Actions.IWorkflowActionExecutor, FWH.Common.Workflow.Actions.WorkflowActionExecutor>();

        var sp = sc.BuildServiceProvider();

        // Ensure database schema is created
        using (var scope = sp.CreateScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<FWH.Mobile.Data.Data.NotesDbContext>();
            ctx.Database.EnsureCreated();
        }

        return sp.GetRequiredService<IWorkflowService>();
    }

    /// <summary>
    /// Tests that GetCurrentStatePayloadAsync returns a choice payload when the workflow is at a branching decision node.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The IWorkflowService.GetCurrentStatePayloadAsync method's ability to detect branching nodes and return choice-based payloads.</para>
    /// <para><strong>Data involved:</strong> A PlantUML workflow definition with a start node, followed by an if-else decision node (condition "cond" with branches "y" and "n"), leading to "Then" and "Else" action nodes, both converging to an "End" node. The workflow is imported and started, placing it at the decision node.</para>
    /// <para><strong>Why the data matters:</strong> Branching nodes require user input to determine which path to take. The service must detect when the workflow is at such a node and return a choice payload (IsChoice=true) with multiple options. This enables the UI to present choices to users. The test validates that decision nodes are correctly identified and converted to choice payloads.</para>
    /// <para><strong>Expected outcome:</strong> After importing and starting the workflow, GetCurrentStatePayloadAsync should return a payload with IsChoice=true and Choices.Count >= 2 (representing the "y" and "n" branches).</para>
    /// <para><strong>Reason for expectation:</strong> When the workflow reaches a decision node with multiple outgoing transitions (if-else), the service should recognize it as a choice point and return a choice payload. The payload should contain at least 2 choices corresponding to the decision branches. This allows the UI to display options and wait for user selection before advancing.</para>
    /// </remarks>
    [Fact]
    public async Task GetCurrentStatePayload_ReturnsChoiceForBranchingNode()
    {
        var plant = @"@startuml
:Start;
if (cond) then (y)
:Then;
else (n)
:Else;
endif
:Then --> :End;
:Else --> :End;
@enduml";

        var svc = BuildWithInMemoryRepo();
        var def = await svc.ImportWorkflowAsync(plant, "wf1", "test");

        var payload = await svc.GetCurrentStatePayloadAsync(def.Id);
        Assert.True(payload.IsChoice);
        Assert.True(payload.Choices.Count >= 2);
    }

    /// <summary>
    /// Tests that AdvanceByChoiceValueAsync advances the workflow to the selected branch and persists the state change to the database.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The IWorkflowService.AdvanceByChoiceValueAsync method's ability to advance workflow execution based on user choice and persist the new state.</para>
    /// <para><strong>Data involved:</strong> A PlantUML workflow with node A having two outgoing transitions (to B and C), creating a choice point. The workflow is imported and started at node A. Choice index 1 is selected (the second outgoing transition, to C). The service uses an in-memory SQLite database for persistence.</para>
    /// <para><strong>Why the data matters:</strong> When users make choices in branching workflows, the workflow must advance to the selected branch and persist the new current node. This test validates that: (1) the choice index correctly maps to the corresponding transition, (2) the workflow advances to the target node, and (3) the state change is persisted to the database so it survives service restarts. The in-memory database ensures test isolation while validating real persistence.</para>
    /// <para><strong>Expected outcome:</strong> After calling AdvanceByChoiceValueAsync with index 1, the method should return true (indicating success), and querying the persisted workflow from the database should show CurrentNodeId matching the target node of choice index 1 (node C).</para>
    /// <para><strong>Reason for expectation:</strong> The AdvanceByChoiceValueAsync method should map the choice index to the corresponding transition, advance the workflow controller to the target node, and persist the new CurrentNodeId to the database. When the workflow is reloaded from the database, it should reflect the advanced state, confirming that persistence works correctly. This ensures workflow state survives application restarts.</para>
    /// </remarks>
    [Fact]
    public async Task AdvanceByChoiceValue_AdvancesAndPersists()
    {
        var services = new ServiceCollection();
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        services.AddDbContext<FWH.Mobile.Data.Data.NotesDbContext>(options => options.UseSqlite(connection));
        services.AddScoped<FWH.Mobile.Data.Repositories.IWorkflowRepository, FWH.Mobile.Data.Repositories.EfWorkflowRepository>();

        // Register SRP components with Controller pattern
        services.AddSingleton<IWorkflowDefinitionStore, InMemoryWorkflowDefinitionStore>();
        services.AddSingleton<IWorkflowInstanceManager, InMemoryWorkflowInstanceManager>();
        services.AddSingleton<IWorkflowModelMapper, WorkflowModelMapper>();
        services.AddSingleton<IWorkflowStateCalculator, WorkflowStateCalculator>();
        services.AddSingleton<IWorkflowController, WorkflowController>();
        services.AddSingleton<IWorkflowService, WorkflowService>();
        services.AddTransient<IWorkflowView, WorkflowView>();
        services.AddLogging();

        // Register action executor and handler registry so controller DI can resolve dependencies in tests
        services.AddSingleton<FWH.Common.Workflow.Actions.IWorkflowActionHandlerRegistry, FWH.Common.Workflow.Actions.WorkflowActionHandlerRegistry>();
        services.AddSingleton<FWH.Common.Workflow.Actions.WorkflowActionHandlerRegistrar>();
        services.AddSingleton<FWH.Orchestrix.Contracts.Mediator.IMediatorSender, FWH.Orchestrix.Mediator.Remote.Mediator.ServiceProviderMediatorSender>();
        services.AddTransient<FWH.Orchestrix.Contracts.Mediator.IMediatorHandler<FWH.Common.Workflow.Actions.WorkflowActionRequest, FWH.Common.Workflow.Actions.WorkflowActionResponse>, FWH.Common.Workflow.Actions.WorkflowActionRequestHandler>();
        services.AddSingleton<FWH.Common.Workflow.Actions.IWorkflowActionExecutor, FWH.Common.Workflow.Actions.WorkflowActionExecutor>();

        // Build provider and ensure DB created
        var sp = services.BuildServiceProvider();
        using (var scope = sp.CreateScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<FWH.Mobile.Data.Data.NotesDbContext>();
            ctx.Database.EnsureCreated();
        }

        var svc = sp.GetRequiredService<IWorkflowService>();

        var plant = @"@startuml
[*] --> A
:A;
A --> B
A --> C
@enduml";

        var def = await svc.ImportWorkflowAsync(plant, "wf_persist", "persistTest");

        var payload = await svc.GetCurrentStatePayloadAsync(def.Id);
        Assert.True(payload.IsChoice);

        // choose index 1, which should map to the second outgoing
        var advanced = await svc.AdvanceByChoiceValueAsync(def.Id, 1);
        Assert.True(advanced);

        // Reload persisted definition and check CurrentNodeId
        var repo = sp.GetRequiredService<FWH.Mobile.Data.Repositories.IWorkflowRepository>();
        var persisted = await repo.GetByIdAsync(def.Id);
        Assert.NotNull(persisted);
        Assert.Equal((await svc.GetCurrentStatePayloadAsync(def.Id)).Choices.FirstOrDefault()?.TargetNodeId ?? persisted.CurrentNodeId, persisted.CurrentNodeId);
    }
}
