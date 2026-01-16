using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;
using FWH.Common.Workflow.Actions;
using FWH.Common.Workflow.Instance;
using FWH.Common.Workflow.Storage;
using FWH.Common.Workflow.Mapping;
using FWH.Common.Workflow.State;
using FWH.Common.Workflow.Views;
using FWH.Common.Workflow.Controllers;
using FWH.Common.Workflow;
using FWH.Mobile.Data.Data;
using FWH.Mobile.Data.Repositories;
using System.Collections.Generic;
using System.Threading;
using FWH.Orchestrix.Contracts.Mediator;
using FWH.Orchestrix.Mediator.Remote.Mediator;

namespace FWH.Common.Workflow.Tests;

public interface IScopedDep
{
    string GetValue();
}

public class ScopedDep : IScopedDep
{
    public string GetValue() => "scoped-value";
}

public class ScopedHandler : IWorkflowActionHandler
{
    private readonly IScopedDep _dep;

    public string Name => "ScopedAction";

    public ScopedHandler(IScopedDep dep)
    {
        _dep = dep;
    }

    public Task<IDictionary<string,string>?> HandleAsync(ActionHandlerContext context, IDictionary<string,string> parameters, CancellationToken cancellationToken = default)
    {
        var updates = new Dictionary<string,string>
        {
            ["fromScoped"] = _dep.GetValue()
        };

        return Task.FromResult<IDictionary<string,string>?>(updates);
    }
}

public class ScopedHandlerTests
{
    [Fact]
    public async Task ScopedHandler_ResolvesScopedService_WhenExecutorCreatesScope()
    {
        var services = new ServiceCollection();

        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        services.AddDbContext<NotesDbContext>(o => o.UseSqlite(connection));
        services.AddScoped<IScopedDep, ScopedDep>();
        services.AddScoped<ScopedHandler>();
        services.AddScoped<IWorkflowActionHandler>(sp => sp.GetRequiredService<ScopedHandler>());

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

        var sp = services.BuildServiceProvider();
        _ = sp.GetRequiredService<WorkflowActionHandlerRegistrar>();

        // create a fake workflow and node with note action
        var def = new FWH.Common.Workflow.Models.WorkflowDefinition("w1", "n", new System.Collections.Generic.List<FWH.Common.Workflow.Models.WorkflowNode> { new FWH.Common.Workflow.Models.WorkflowNode("A","A","{\"action\":\"ScopedAction\", \"params\": {}}") }, new System.Collections.Generic.List<FWH.Common.Workflow.Models.Transition>(), new System.Collections.Generic.List<FWH.Common.Workflow.Models.StartPoint>());

        var executor = sp.GetRequiredService<IWorkflowActionExecutor>();
        var executed = await executor.ExecuteAsync("w1", def.Nodes[0], def, CancellationToken.None);
        Assert.True(executed);

        var inst = sp.GetRequiredService<IWorkflowInstanceManager>();
        var vars = inst.GetVariables("w1");
        Assert.NotNull(vars);
        Assert.Equal("scoped-value", vars["fromScoped"]);
    }
}
