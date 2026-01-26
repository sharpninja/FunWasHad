using System.Collections.Concurrent;
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
using Microsoft.Extensions.Options;
using Xunit;

namespace FWH.Common.Workflow.Tests;

public class ActionExecutionTests
{
    private IWorkflowService BuildWithInMemoryRepo(out ServiceProvider sp)
    {
        var services = new ServiceCollection();

        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        services.AddDbContext<NotesDbContext>(options => options.UseSqlite(connection));
        services.AddScoped<IWorkflowRepository, FWH.Mobile.Data.Repositories.EfWorkflowRepository>();

        services.AddSingleton<IWorkflowDefinitionStore, InMemoryWorkflowDefinitionStore>();
        services.AddSingleton<IWorkflowInstanceManager, InMemoryWorkflowInstanceManager>();
        services.AddSingleton<IWorkflowModelMapper, WorkflowModelMapper>();
        services.AddSingleton<IWorkflowStateCalculator, WorkflowStateCalculator>();
        services.AddSingleton<IWorkflowActionHandlerRegistry, WorkflowActionHandlerRegistry>();
        services.AddSingleton<WorkflowActionHandlerRegistrar>();

        // Register mediator sender and handler (handler uses registry, so tests work with registry path)
        services.AddSingleton<IMediatorSender, ServiceProviderMediatorSender>();
        services.AddTransient<IMediatorHandler<WorkflowActionRequest, WorkflowActionResponse>, WorkflowActionRequestHandler>();

        services.AddSingleton<IWorkflowActionExecutor>(sp => new WorkflowActionExecutor(sp, sp.GetRequiredService<IMediatorSender>(), Options.Create(new WorkflowActionExecutorOptions { ExecuteHandlersInBackground = false })));
        services.AddSingleton<IWorkflowController, WorkflowController>();
        services.AddSingleton<IWorkflowService, WorkflowService>();
        services.AddTransient<IWorkflowView, WorkflowView>();
        services.AddLogging();

        // Register a test handler for SendMessage that returns a variable update using fluent helper
        services.AddWorkflowActionHandler("SendMessage", async (ctx, p, ct) =>
        {
            ct.ThrowIfCancellationRequested();
            var updates = new ConcurrentDictionary<string, string> { ["lastMessage"] = p.TryGetValue("text", out var t) ? t : string.Empty };
            await Task.CompletedTask.ConfigureAwait(true);
            return updates;
        });

        sp = services.BuildServiceProvider();

        // Manually ensure registrar picked up handlers (constructed in DI) in test environment
        _ = sp.GetService<WorkflowActionHandlerRegistrar>();

        using (var scope = sp.CreateScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<NotesDbContext>();
            ctx.Database.EnsureCreated();
        }

        return sp.GetRequiredService<IWorkflowService>();
    }

    /// <summary>
    /// Tests that workflow action execution correctly resolves template variables and updates workflow variables.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The workflow action execution system's ability to resolve template variables (e.g., {{userName}}) in action parameters and update workflow variables as a result of action execution.</para>
    /// <para><strong>Data involved:</strong> A PlantUML workflow definition with a SendMessage action containing a template variable {{userName}}. The workflow variable "userName" is pre-set to "Alice" before workflow import. The action parameters include {"text": "Hello, {{userName}}"} which should resolve to "Hello, Alice".</para>
    /// <para><strong>Why the data matters:</strong> Template resolution is critical for dynamic workflow behavior - actions need to use workflow variables to personalize messages and behavior. The test validates that the template resolution works correctly and that actions can update workflow state (setting "lastMessage" variable).</para>
    /// <para><strong>Expected outcome:</strong> After workflow execution, the "lastMessage" variable should be set to "Hello, Alice" (the resolved template), and the workflow should advance to node B.</para>
    /// <para><strong>Reason for expectation:</strong> The SendMessage handler is configured to set the "lastMessage" variable with the resolved text parameter. Template resolution should replace {{userName}} with "Alice" from the workflow variables. The workflow should auto-advance from A to B because node A has a single outgoing transition.</para>
    /// </remarks>
    [Fact]
    public async Task ExecuteJsonActionSendMessageHandlerResolvesTemplateAndUpdatesVariables()
    {
        var svc = BuildWithInMemoryRepo(out var sp);
        var wm = sp.GetRequiredService<IWorkflowInstanceManager>();

        // Build a simple plantuml with an action node that sends message using {{userName}}
        var plant = "@startuml\n[*] --> A\n:A\nnote right: {\"action\": \"SendMessage\", \"params\": { \"text\": \"Hello, {{userName}}\" }}\nA --> B\n:B\n@enduml";

        // Pre-create id and set variable before importing so Import will execute action with available variable
        var id = "act1";
        wm.SetVariable(id, "userName", "Alice");
        var def = await svc.ImportWorkflowAsync(plant, id, "actionTest").ConfigureAwait(true);

        // Verify variable set by handler (action runs asynchronously); wait until handler sets variable or timeout
        IDictionary<string, string>? vars = null;
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (sw.Elapsed < System.TimeSpan.FromSeconds(2))
        {
            vars = wm.GetVariables(def.Id);
            if (vars != null && vars.ContainsKey("lastMessage")) break;
            await Task.Delay(50).ConfigureAwait(true);
        }

        Assert.NotNull(vars);
        Assert.True(vars.ContainsKey("lastMessage"));
        Assert.Equal("Hello, Alice", vars["lastMessage"]);

        // After start, controller should have auto-advanced to B because single outgoing
        var current = sp.GetRequiredService<IWorkflowController>().GetCurrentNodeId(def.Id);
        Assert.Equal("B", current);
    }

    /// <summary>
    /// Tests that template resolution correctly replaces multiple template variables in action parameters.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The ResolveTemplates method's ability to replace multiple template variables (e.g., {{first}}, {{last}}, {{id}}) in a single parameter dictionary.</para>
    /// <para><strong>Data involved:</strong> A parameters dictionary with two entries: "text" containing "Hi {{first}} {{last}}" and "meta" containing "id:{{id}}". A variables dictionary with "first" = "Jane", "last" = "Doe", and "id" = "42". These represent typical workflow action parameters that need template resolution.</para>
    /// <para><strong>Why the data matters:</strong> Real-world workflow actions often contain multiple template variables in multiple parameters. This test ensures that all templates are resolved correctly across all parameters, not just the first one. The test data includes templates in different positions (beginning, middle, end) to validate comprehensive replacement.</para>
    /// <para><strong>Expected outcome:</strong> The resolved parameters should have "text" = "Hi Jane Doe" and "meta" = "id:42", with all template variables replaced by their corresponding values.</para>
    /// <para><strong>Reason for expectation:</strong> The ResolveTemplates method should iterate through all parameters and replace all {{variableName}} patterns with values from the variables dictionary. Each template variable should be replaced exactly once, and multiple templates in the same string should all be resolved.</para>
    /// </remarks>
    [Fact]
    public Task ParameterResolutionReplacesMultipleTemplates()
    {
        var parameters = new System.Collections.Generic.Dictionary<string, string>
        {
            ["text"] = "Hi {{first}} {{last}}",
            ["meta"] = "id:{{id}}"
        };

        var vars = new System.Collections.Generic.Dictionary<string, string>
        {
            ["first"] = "Jane",
            ["last"] = "Doe",
            ["id"] = "42"
        };

        var resolved = typeof(WorkflowActionExecutor)
            .GetMethod("ResolveTemplates", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .Invoke(null, new object[] { parameters, vars }) as System.Collections.Generic.IDictionary<string, string>;

        Assert.NotNull(resolved);
        Assert.Equal("Hi Jane Doe", resolved["text"]);
        Assert.Equal("id:42", resolved["meta"]);
        return Task.CompletedTask;
    }
}
