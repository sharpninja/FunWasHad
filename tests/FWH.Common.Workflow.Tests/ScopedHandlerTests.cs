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
    /// <summary>
    /// Tests that workflow action handlers registered as scoped services can correctly resolve scoped dependencies when the executor creates a service scope.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The WorkflowActionExecutor's ability to create service scopes and resolve scoped action handlers with scoped dependencies.</para>
    /// <para><strong>Data involved:</strong> A ScopedHandler registered as a scoped service that depends on IScopedDep (also scoped). The handler sets a workflow variable "fromScoped" to the value returned by the dependency ("scoped-value"). A workflow with action "ScopedAction" is executed.</para>
    /// <para><strong>Why the data matters:</strong> Action handlers may need scoped dependencies (e.g., database contexts, per-request services) that should be disposed after handler execution. The executor must create a service scope for each handler execution to properly resolve and dispose scoped dependencies. This tests dependency injection scoping works correctly in the workflow execution context.</para>
    /// <para><strong>Expected outcome:</strong> ExecuteAsync should return true, and the workflow variable "fromScoped" should be set to "scoped-value", confirming the handler resolved its scoped dependency correctly.</para>
    /// <para><strong>Reason for expectation:</strong> The executor should create a service scope before resolving the handler, allowing scoped services to be resolved. The handler should receive its IScopedDep dependency and be able to call its methods. The workflow variable being set to "scoped-value" confirms the dependency was resolved and the handler executed successfully. This validates that scoped service resolution works correctly in the workflow execution pipeline.</para>
    /// </remarks>
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
