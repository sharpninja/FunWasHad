using Xunit;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using FWH.Common.Workflow;
using FWH.Common.Workflow.Actions;
using FWH.Common.Workflow.Controllers;
using FWH.Common.Workflow.Instance;
using FWH.Mobile.Data.Repositories;
using FWH.Mobile.Data.Data;
using System.Linq;
using FWH.Common.Workflow.Storage;
using FWH.Common.Workflow.Mapping;
using FWH.Common.Workflow.State;
using FWH.Common.Workflow.Views;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading;
using FWH.Common.Workflow.Extensions;
using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using FWH.Common.Workflow.Actions;

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
        services.AddSingleton<IWorkflowActionExecutor>(sp => new WorkflowActionExecutor(sp, sp.GetRequiredService<IWorkflowActionHandlerRegistry>(), Options.Create(new WorkflowActionExecutorOptions { ExecuteHandlersInBackground = false })));
        services.AddSingleton<IWorkflowController, WorkflowController>();
        services.AddSingleton<IWorkflowService, WorkflowService>();
        services.AddTransient<IWorkflowView, WorkflowView>();
        services.AddLogging();

        // Register a test handler for SendMessage that returns a variable update using fluent helper
        services.AddWorkflowActionHandler("SendMessage", async (ctx, p, ct) =>
        {
            ct.ThrowIfCancellationRequested();
            var updates = new ConcurrentDictionary<string,string> { ["lastMessage"] = p.TryGetValue("text", out var t) ? t : string.Empty };
            await Task.CompletedTask;
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

    [Fact]
    public async Task ExecuteJsonAction_SendMessageHandlerResolvesTemplate_AndUpdatesVariables()
    {
        var svc = BuildWithInMemoryRepo(out var sp);
        var wm = sp.GetRequiredService<IWorkflowInstanceManager>();

        // Build a simple plantuml with an action node that sends message using {{userName}}
        var plant = "@startuml\n[*] --> A\n:A\nnote right: {\"action\": \"SendMessage\", \"params\": { \"text\": \"Hello, {{userName}}\" }}\nA --> B\n:B\n@enduml";

        // Pre-create id and set variable before importing so Import will execute action with available variable
        var id = "act1";
        wm.SetVariable(id, "userName", "Alice");
        var def = await svc.ImportWorkflowAsync(plant, id, "actionTest");

        // Verify variable set by handler (action runs asynchronously); wait until handler sets variable or timeout
        IDictionary<string,string>? vars = null;
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (sw.Elapsed < System.TimeSpan.FromSeconds(2))
        {
            vars = wm.GetVariables(def.Id);
            if (vars != null && vars.ContainsKey("lastMessage")) break;
            await Task.Delay(50);
        }

        Assert.NotNull(vars);
        Assert.True(vars.ContainsKey("lastMessage"));
        Assert.Equal("Hello, Alice", vars["lastMessage"]);

        // After start, controller should have auto-advanced to B because single outgoing
        var current = sp.GetRequiredService<IWorkflowController>().GetCurrentNodeId(def.Id);
        Assert.Equal("B", current);
    }

    [Fact]
    public async Task ParameterResolution_ReplacesMultipleTemplates()
    {
        var parameters = new System.Collections.Generic.Dictionary<string,string>
        {
            ["text"] = "Hi {{first}} {{last}}",
            ["meta"] = "id:{{id}}"
        };

        var vars = new System.Collections.Generic.Dictionary<string,string>
        {
            ["first"] = "Jane",
            ["last"] = "Doe",
            ["id"] = "42"
        };

        var resolved = typeof(WorkflowActionExecutor)
            .GetMethod("ResolveTemplates", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .Invoke(null, new object[] { parameters, vars }) as System.Collections.Generic.IDictionary<string,string>;

        Assert.NotNull(resolved);
        Assert.Equal("Hi Jane Doe", resolved["text"]);
        Assert.Equal("id:42", resolved["meta"]);
    }
}
