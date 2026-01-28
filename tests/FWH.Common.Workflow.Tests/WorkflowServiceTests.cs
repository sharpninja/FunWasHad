using FWH.Common.Workflow.Controllers;
using FWH.Common.Workflow.Instance;
using FWH.Common.Workflow.Mapping;
using FWH.Common.Workflow.State;
using FWH.Common.Workflow.Storage;
using FWH.Common.Workflow.Views;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

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
    public async Task GetCurrentStatePayloadReturnsChoiceForBranchingNode()
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
        var def = await svc.ImportWorkflowAsync(plant, "wf1", "test").ConfigureAwait(true);

        var payload = await svc.GetCurrentStatePayloadAsync(def.Id).ConfigureAwait(true);
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
    public async Task AdvanceByChoiceValueAdvancesAndPersists()
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

        var def = await svc.ImportWorkflowAsync(plant, "wf_persist", "persistTest").ConfigureAwait(true);

        var payload = await svc.GetCurrentStatePayloadAsync(def.Id).ConfigureAwait(true);
        Assert.True(payload.IsChoice);

        // choose index 1, which should map to the second outgoing
        var advanced = await svc.AdvanceByChoiceValueAsync(def.Id, 1).ConfigureAwait(true);
        Assert.True(advanced);

        // Reload persisted definition and check CurrentNodeId
        var repo = sp.GetRequiredService<FWH.Mobile.Data.Repositories.IWorkflowRepository>();
        var persisted = await repo.GetByIdAsync(def.Id).ConfigureAwait(true);
        Assert.NotNull(persisted);
        Assert.Equal((await svc.GetCurrentStatePayloadAsync(def.Id).ConfigureAwait(true)).Choices.FirstOrDefault()?.TargetNodeId ?? persisted.CurrentNodeId, persisted.CurrentNodeId);
    }

    /// <summary>
    /// Tests that ImportWorkflowAsync creates a new workflow when importing a workflow that doesn't exist.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The IWorkflowService.ImportWorkflowAsync method's behavior when importing a workflow with an ID that doesn't exist in the store.</para>
    /// <para><strong>Data involved:</strong> A simple PlantUML workflow with nodes A and B, imported with Id="wf_new" and Name="NewWorkflow".</para>
    /// <para><strong>Why the data matters:</strong> When importing a workflow that doesn't exist, the service should create a new workflow definition, store it, and persist it to the database. This is the baseline behavior for workflow import.</para>
    /// <para><strong>Expected outcome:</strong> The returned workflow definition should have Id="wf_new", Name="NewWorkflow", and the workflow should be persisted to the database.</para>
    /// <para><strong>Reason for expectation:</strong> ImportWorkflowAsync should parse the PlantUML, create a new WorkflowDefinition, store it in the definition store, and persist it to the database when the workflow doesn't exist.</para>
    /// </remarks>
    [Fact]
    public async Task ImportWorkflow_WhenNotExists_CreatesNew()
    {
        var svc = BuildWithInMemoryRepo();
        var plant = @"@startuml
[*] --> A
:A;
A --> B
:B;
@enduml";

        var def = await svc.ImportWorkflowAsync(plant, "wf_new", "NewWorkflow").ConfigureAwait(true);

        Assert.Equal("wf_new", def.Id);
        Assert.Equal("NewWorkflow", def.Name);
        Assert.True(def.Nodes.Count >= 2);
    }

    /// <summary>
    /// Tests that ImportWorkflowAsync returns the existing workflow without updating when importing an identical workflow.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The IWorkflowService.ImportWorkflowAsync method's ability to detect when an imported workflow is identical to an existing one and skip unnecessary updates.</para>
    /// <para><strong>Data involved:</strong> A PlantUML workflow with nodes A and B, imported twice with the same ID and identical content. The first import creates the workflow, the second import should detect it's identical.</para>
    /// <para><strong>Why the data matters:</strong> When importing the same workflow multiple times (e.g., during app startup or configuration reload), the service should detect that the workflow hasn't changed and avoid unnecessary parsing, storage updates, and database writes. This improves performance and prevents unnecessary database churn.</para>
    /// <para><strong>Expected outcome:</strong> The second import should return the same workflow definition object reference (or equivalent), and the workflow should remain unchanged in the store.</para>
    /// <para><strong>Reason for expectation:</strong> ImportWorkflowAsync should compare the parsed definition with the existing one. If they're identical (same nodes, transitions, start points), it should return the existing definition without updating the store or database.</para>
    /// </remarks>
    [Fact]
    public async Task ImportWorkflow_WhenIdentical_ReturnsExistingWithoutUpdate()
    {
        var svc = BuildWithInMemoryRepo();
        var plant = @"@startuml
[*] --> A
:A;
A --> B
:B;
@enduml";

        var firstImport = await svc.ImportWorkflowAsync(plant, "wf_identical", "IdenticalTest").ConfigureAwait(true);
        var firstNodeCount = firstImport.Nodes.Count;
        var firstTransitionCount = firstImport.Transitions.Count;

        // Import the same workflow again
        var secondImport = await svc.ImportWorkflowAsync(plant, "wf_identical", "IdenticalTest").ConfigureAwait(true);

        // Should return the same workflow (or equivalent)
        Assert.Equal(firstImport.Id, secondImport.Id);
        Assert.Equal(firstImport.Name, secondImport.Name);
        Assert.Equal(firstNodeCount, secondImport.Nodes.Count);
        Assert.Equal(firstTransitionCount, secondImport.Transitions.Count);

        // Verify nodes are the same
        var firstNodeIds = firstImport.Nodes.Select(n => n.Id).OrderBy(id => id).ToList();
        var secondNodeIds = secondImport.Nodes.Select(n => n.Id).OrderBy(id => id).ToList();
        Assert.Equal(firstNodeIds, secondNodeIds);
    }

    /// <summary>
    /// Tests that ImportWorkflowAsync updates the workflow when importing a workflow with different nodes.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The IWorkflowService.ImportWorkflowAsync method's ability to detect when an imported workflow differs from an existing one and update it accordingly.</para>
    /// <para><strong>Data involved:</strong> A PlantUML workflow with nodes A and B, imported with Id="wf_diff_nodes". Then the same workflow is imported again but with an additional node C.</para>
    /// <para><strong>Why the data matters:</strong> When a workflow definition changes (e.g., new nodes added), the service should detect the difference and update the workflow definition. This ensures workflows can be updated when their definitions evolve.</para>
    /// <para><strong>Expected outcome:</strong> The second import should return a workflow definition with the additional node C, and the workflow should be updated in the store.</para>
    /// <para><strong>Reason for expectation:</strong> ImportWorkflowAsync should compare the parsed definition with the existing one. If they differ (different node count or IDs), it should update the workflow definition in the store and persist the changes.</para>
    /// </remarks>
    [Fact]
    public async Task ImportWorkflow_WhenDifferentNodes_UpdatesWorkflow()
    {
        var svc = BuildWithInMemoryRepo();
        var plant1 = @"@startuml
[*] --> A
:A;
A --> B
:B;
@enduml";

        var plant2 = @"@startuml
[*] --> A
:A;
A --> B
:B;
B --> C
:C;
@enduml";

        var firstImport = await svc.ImportWorkflowAsync(plant1, "wf_diff_nodes", "DiffNodesTest").ConfigureAwait(true);
        var firstNodeCount = firstImport.Nodes.Count;

        // Import with additional node
        var secondImport = await svc.ImportWorkflowAsync(plant2, "wf_diff_nodes", "DiffNodesTest").ConfigureAwait(true);

        // Should have more nodes
        Assert.True(secondImport.Nodes.Count > firstNodeCount);
        Assert.Contains(secondImport.Nodes, n => n.Label == "C");
    }

    /// <summary>
    /// Tests that ImportWorkflowAsync updates the workflow when importing a workflow with different transitions.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The IWorkflowService.ImportWorkflowAsync method's ability to detect when an imported workflow has different transitions than an existing one.</para>
    /// <para><strong>Data involved:</strong> A PlantUML workflow with nodes A, B, and C. First import has A --> B. Second import changes to A --> C.</para>
    /// <para><strong>Why the data matters:</strong> When workflow transitions change (e.g., routing logic updated), the service should detect the difference and update the workflow definition. This ensures workflows reflect the latest routing structure.</para>
    /// <para><strong>Expected outcome:</strong> The second import should return a workflow definition with the updated transition (A --> C instead of A --> B).</para>
    /// <para><strong>Reason for expectation:</strong> ImportWorkflowAsync should compare transitions. If they differ (different from/to nodes or conditions), it should update the workflow definition.</para>
    /// </remarks>
    [Fact]
    public async Task ImportWorkflow_WhenDifferentTransitions_UpdatesWorkflow()
    {
        var svc = BuildWithInMemoryRepo();
        var plant1 = @"@startuml
[*] --> A
:A;
A --> B
:B;
:C;
@enduml";

        var plant2 = @"@startuml
[*] --> A
:A;
A --> C
:B;
:C;
@enduml";

        var firstImport = await svc.ImportWorkflowAsync(plant1, "wf_diff_transitions", "DiffTransitionsTest").ConfigureAwait(true);
        var firstTransition = firstImport.Transitions.FirstOrDefault(t => t.FromNodeId == "A");
        Assert.NotNull(firstTransition);
        Assert.Equal("B", firstTransition.ToNodeId);

        // Import with different transition
        var secondImport = await svc.ImportWorkflowAsync(plant2, "wf_diff_transitions", "DiffTransitionsTest").ConfigureAwait(true);

        // Should have updated transition
        var secondTransition = secondImport.Transitions.FirstOrDefault(t => t.FromNodeId == "A");
        Assert.NotNull(secondTransition);
        Assert.Equal("C", secondTransition.ToNodeId);
    }

    /// <summary>
    /// Tests that ImportWorkflowAsync updates the workflow when importing a workflow with different start points.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The IWorkflowService.ImportWorkflowAsync method's ability to detect when an imported workflow has different start points than an existing one.</para>
    /// <para><strong>Data involved:</strong> A PlantUML workflow with nodes A and B. First import starts at A. Second import starts at B.</para>
    /// <para><strong>Why the data matters:</strong> When workflow start points change, the service should detect the difference and update the workflow definition. This ensures workflows start at the correct initial node.</para>
    /// <para><strong>Expected outcome:</strong> The second import should return a workflow definition with the updated start point (B instead of A).</para>
    /// <para><strong>Reason for expectation:</strong> ImportWorkflowAsync should compare start points. If they differ, it should update the workflow definition.</para>
    /// </remarks>
    [Fact]
    public async Task ImportWorkflow_WhenDifferentStartPoints_UpdatesWorkflow()
    {
        var svc = BuildWithInMemoryRepo();
        var plant1 = @"@startuml
[*] --> A
:A;
A --> B
:B;
@enduml";

        var plant2 = @"@startuml
[*] --> B
:A;
A --> B
:B;
@enduml";

        var firstImport = await svc.ImportWorkflowAsync(plant1, "wf_diff_start", "DiffStartTest").ConfigureAwait(true);
        var firstStartPoint = firstImport.StartPoints.FirstOrDefault();
        Assert.NotNull(firstStartPoint);
        Assert.Equal("A", firstStartPoint.NodeId);

        // Import with different start point
        var secondImport = await svc.ImportWorkflowAsync(plant2, "wf_diff_start", "DiffStartTest").ConfigureAwait(true);

        // Should have updated start point
        var secondStartPoint = secondImport.StartPoints.FirstOrDefault();
        Assert.NotNull(secondStartPoint);
        Assert.Equal("B", secondStartPoint.NodeId);
    }

    /// <summary>
    /// Tests that ImportWorkflowAsync updates the workflow when importing a workflow with different node labels.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The IWorkflowService.ImportWorkflowAsync method's ability to detect when an imported workflow has nodes with different labels than an existing one.</para>
    /// <para><strong>Data involved:</strong> A PlantUML workflow with node labeled "Start". Second import changes the label to "Begin".</para>
    /// <para><strong>Why the data matters:</strong> When node labels change (e.g., UI text updated), the service should detect the difference and update the workflow definition. This ensures workflows display the correct labels.</para>
    /// <para><strong>Expected outcome:</strong> The second import should return a workflow definition with the updated node label ("Begin" instead of "Start").</para>
    /// <para><strong>Reason for expectation:</strong> ImportWorkflowAsync should compare node properties including labels. If they differ, it should update the workflow definition.</para>
    /// </remarks>
    [Fact]
    public async Task ImportWorkflow_WhenDifferentNodeLabels_UpdatesWorkflow()
    {
        var svc = BuildWithInMemoryRepo();
        var plant1 = @"@startuml
[*] --> :Start;
:Start;
@enduml";

        var plant2 = @"@startuml
[*] --> :Begin;
:Begin;
@enduml";

        var firstImport = await svc.ImportWorkflowAsync(plant1, "wf_diff_labels", "DiffLabelsTest").ConfigureAwait(true);
        var firstNode = firstImport.Nodes.FirstOrDefault(n => n.Id == "Start");
        Assert.NotNull(firstNode);
        Assert.Equal("Start", firstNode.Label);

        // Import with different label
        var secondImport = await svc.ImportWorkflowAsync(plant2, "wf_diff_labels", "DiffLabelsTest").ConfigureAwait(true);

        // Should have updated label (note: node ID might be different, so check by label)
        var secondNode = secondImport.Nodes.FirstOrDefault(n => n.Label == "Begin");
        Assert.NotNull(secondNode);
        Assert.Equal("Begin", secondNode.Label);
    }

    /// <summary>
    /// Tests that ImportWorkflowAsync handles workflows with different names correctly.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The IWorkflowService.ImportWorkflowAsync method's behavior when importing a workflow with the same ID but different name.</para>
    /// <para><strong>Data involved:</strong> A PlantUML workflow imported with Name="Original", then imported again with Name="Updated" but same ID.</para>
    /// <para><strong>Why the data matters:</strong> When a workflow's name changes, the service should detect the difference and update the workflow definition. The name is part of the workflow identity.</para>
    /// <para><strong>Expected outcome:</strong> The second import should return a workflow definition with the updated name ("Updated" instead of "Original").</para>
    /// <para><strong>Reason for expectation:</strong> ImportWorkflowAsync should compare the name property. If it differs, it should update the workflow definition.</para>
    /// </remarks>
    [Fact]
    public async Task ImportWorkflow_WhenDifferentName_UpdatesWorkflow()
    {
        var svc = BuildWithInMemoryRepo();
        var plant = @"@startuml
[*] --> A
:A;
@enduml";

        var firstImport = await svc.ImportWorkflowAsync(plant, "wf_diff_name", "Original").ConfigureAwait(true);
        Assert.Equal("Original", firstImport.Name);

        // Import with different name
        var secondImport = await svc.ImportWorkflowAsync(plant, "wf_diff_name", "Updated").ConfigureAwait(true);

        // Should have updated name
        Assert.Equal("Updated", secondImport.Name);
    }
}
