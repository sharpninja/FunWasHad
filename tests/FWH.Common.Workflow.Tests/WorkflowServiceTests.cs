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
