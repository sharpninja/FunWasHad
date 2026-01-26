using FWH.Common.Workflow.Actions;
using FWH.Common.Workflow.Controllers;
using FWH.Common.Workflow.Extensions;
using FWH.Common.Workflow.Instance;
using FWH.Common.Workflow.Mapping;
using FWH.Common.Workflow.State;
using FWH.Common.Workflow.Storage;
using FWH.Common.Workflow.Views;
using FWH.Mobile.Data.Data;
using FWH.Mobile.Data.Repositories;
using FWH.Orchestrix.Contracts.Mediator;
using FWH.Orchestrix.Mediator.Remote.Mediator;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FWH.Common.Workflow.Tests;

public class WorkflowPersistenceActionTests
{
    /// <summary>
    /// Tests that workflow state (CurrentNodeId) is persisted to the database after action execution and automatic workflow advancement.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The workflow persistence mechanism's ability to save the current node ID to the database after action execution causes automatic workflow advancement.</para>
    /// <para><strong>Data involved:</strong> A PlantUML workflow with nodes A (containing SendMessage action) and B. The workflow is imported, which triggers action execution and auto-advancement from A to B. The test uses an in-memory SQLite database to verify persistence.</para>
    /// <para><strong>Why the data matters:</strong> Workflow state must be persisted so it survives application restarts. When actions execute and workflows auto-advance (e.g., single outgoing transition), the new current node must be saved to the database. This test validates that the persistence layer correctly captures state changes from action execution.</para>
    /// <para><strong>Expected outcome:</strong> After importing the workflow and waiting for action execution (500ms delay), querying the persisted workflow from the database should show CurrentNodeId="B", indicating the workflow advanced and the state was saved.</para>
    /// <para><strong>Reason for expectation:</strong> ImportWorkflowAsync should trigger workflow execution, which executes the action at node A and then auto-advances to B (since A has a single outgoing transition). The WorkflowController should persist the new CurrentNodeId to the database. Querying the repository confirms that the state change was successfully saved, ensuring workflow state survives restarts.</para>
    /// </remarks>
    [Fact]
    public async Task ActionExecutionPersistsStateAfterAutoAdvance()
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
        // Register action handler registry and registrar required by WorkflowActionExecutor
        services.AddSingleton<FWH.Common.Workflow.Actions.IWorkflowActionHandlerRegistry, FWH.Common.Workflow.Actions.WorkflowActionHandlerRegistry>();
        services.AddSingleton<FWH.Common.Workflow.Actions.WorkflowActionHandlerRegistrar>();

        // Register mediator sender and handler
        services.AddLogging();
        services.AddSingleton<IMediatorSender, ServiceProviderMediatorSender>();
        services.AddTransient<IMediatorHandler<WorkflowActionRequest, WorkflowActionResponse>, WorkflowActionRequestHandler>();

        // Register a dummy SendMessage handler so the workflow can auto-advance
        services.AddWorkflowActionHandler("SendMessage", (ctx, p, ct) =>
        {
            // Simple no-op handler
            return Task.FromResult<IDictionary<string, string>?>(new Dictionary<string, string>());
        });

        services.AddSingleton<IWorkflowActionExecutor, WorkflowActionExecutor>();
        services.AddSingleton<IWorkflowController, WorkflowController>();
        services.AddSingleton<IWorkflowService, WorkflowService>();
        services.AddTransient<IWorkflowView, WorkflowView>();
        services.AddLogging();

        var sp = services.BuildServiceProvider();

        // Ensure registrar picks up handlers
        _ = sp.GetService<FWH.Common.Workflow.Actions.WorkflowActionHandlerRegistrar>();

        using (var scope = sp.CreateScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<NotesDbContext>();
            ctx.Database.EnsureCreated();
        }

        var svc = sp.GetRequiredService<IWorkflowService>();
        var repo = sp.GetRequiredService<IWorkflowRepository>();

        var plant = "@startuml\n[*] --> A\n:A\nnote right: {\"action\": \"SendMessage\", \"params\": { \"text\": \"Hello\" }}\nA --> B\n:B\n@enduml";

        var def = await svc.ImportWorkflowAsync(plant, "persistAction", "persistAction").ConfigureAwait(true);

        // Wait for action to execute and workflow to advance (action execution is async)
        await Task.Delay(500).ConfigureAwait(true);

        // After import and StartInstance, the controller should have executed action and auto-advanced
        var persisted = await repo.GetByIdAsync(def.Id).ConfigureAwait(true);
        Assert.NotNull(persisted);
        Assert.Equal("B", persisted!.CurrentNodeId);
    }
}
