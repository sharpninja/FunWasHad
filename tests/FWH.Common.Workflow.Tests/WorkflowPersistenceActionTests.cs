using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using FWH.Common.Workflow;
using FWH.Common.Workflow.Instance;
using FWH.Mobile.Data.Data;
using FWH.Mobile.Data.Repositories;
using System.Linq;
using FWH.Common.Workflow.Storage;
using FWH.Common.Workflow.Mapping;
using FWH.Common.Workflow.State;
using FWH.Common.Workflow.Actions;
using FWH.Common.Workflow.Controllers;
using FWH.Common.Workflow.Views;
using FWH.Common.Workflow.Extensions;
using System.Collections.Generic;
using FWH.Orchestrix.Contracts.Mediator;
using FWH.Orchestrix.Mediator.Remote.Mediator;

namespace FWH.Common.Workflow.Tests;

public class WorkflowPersistenceActionTests
{
    [Fact]
    public async Task ActionExecution_PersistsStateAfterAutoAdvance()
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

        var def = await svc.ImportWorkflowAsync(plant, "persistAction", "persistAction");

        // Wait for action to execute and workflow to advance (action execution is async)
        await Task.Delay(500);

        // After import and StartInstance, the controller should have executed action and auto-advanced
        var persisted = await repo.GetByIdAsync(def.Id);
        Assert.NotNull(persisted);
        Assert.Equal("B", persisted!.CurrentNodeId);
    }
}
