using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using FWH.Common.Workflow;
using FWH.Common.Workflow.Extensions;
using FWH.Common.Chat;
using FWH.Common.Chat.ViewModels;
using FWH.Common.Chat.Extensions;
using FWH.Common.Location.Extensions;
using FWH.Common.Chat.Tests.TestFixtures;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using FWH.Mobile.Data.Data;
using FWH.Mobile.Data.Repositories;
using System.Linq;
using FWH.Common.Workflow.Logging;
using System.Collections.Generic;
using FWH.Common.Chat.Conversion;
using FWH.Common.Chat.Duplicate;

namespace FWH.Common.Chat.Tests;

public class ChatServiceIntegrationTests : IClassFixture<SqliteTestFixture>
{
    private readonly SqliteTestFixture _fixture;

    public ChatServiceIntegrationTests(SqliteTestFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Tests the end-to-end integration of ChatService rendering workflow state, appending entries to the chat list, and persisting workflow state changes.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The complete integration flow from workflow import through ChatService rendering to chat list updates and workflow state persistence.</para>
    /// <para><strong>Data involved:</strong> A PlantUML workflow with nodes A (action node) and B, C (branching targets). The workflow is imported, then ChatService.RenderWorkflowStateAsync is called to render the current state. The test uses a SQLite database for persistence and a logger provider to capture log entries.</para>
    /// <para><strong>Why the data matters:</strong> This is an integration test that validates the entire workflow-to-chat pipeline works correctly. It ensures that workflow definitions can be imported, rendered to the chat UI, and that user interactions (choice selections) properly advance workflows and persist state. This tests the real-world usage scenario where users interact with workflows through the chat interface.</para>
    /// <para><strong>Expected outcome:</strong> After rendering, the chat list should contain exactly one entry (a ChoiceChatEntry) with 2 choices (corresponding to transitions A→B and A→C).</para>
    /// <para><strong>Reason for expectation:</strong> RenderWorkflowStateAsync should query the workflow's current state, detect that node A has multiple outgoing transitions (making it a choice point), convert it to a ChoiceChatEntry, and append it to the chat list. The 2 choices correspond to the two transitions from A. This validates that the workflow-to-chat conversion works correctly and the UI is updated with the workflow state.</para>
    /// </remarks>
    [Fact]
    public async Task RenderWorkflowStateAsync_EndToEndAppendsEntriesAndPersists()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddProvider(_fixture.LoggerProvider));
        services.AddDbContext<NotesDbContext>(options => options.UseSqlite(_fixture.Connection));
        services.AddScoped<IWorkflowRepository, EfWorkflowRepository>();

        // Register workflow and chat services using extension methods
        services.AddWorkflowServices();
        services.AddChatServices();

        // Register location services with in-memory config (not database for tests)
        services.AddLocationServicesWithInMemoryConfig(options =>
        {
            options.DefaultRadiusMeters = 1000;
            options.MaxRadiusMeters = 5000;
            options.MinRadiusMeters = 50;
        });

        var sp = services.BuildServiceProvider();

        var wfSvc = sp.GetRequiredService<IWorkflowService>();
        var chatSvc = sp.GetRequiredService<ChatService>();
        var chatList = sp.GetRequiredService<ChatListViewModel>();
        var repo = sp.GetRequiredService<IWorkflowRepository>();

        // Import a simple branching workflow
        var plant = @"@startuml
[*] --> A
:A;
A --> B
A --> C
@enduml";

        var def = await wfSvc.ImportWorkflowAsync(plant, "wf_integ2", "IntegrationTest");

        // Render initial state to chat
        await chatSvc.RenderWorkflowStateAsync(def.Id);

        // After render, chat list should have one entry (choice)
        Assert.Single(chatList.Entries);

        var choiceEntry = chatList.Entries[0] as FWH.Common.Chat.ViewModels.ChoiceChatEntry;
        Assert.NotNull(choiceEntry);
        Assert.Equal(2, choiceEntry!.Choices.Count);

        // Simulate selecting first choice
        var first = choiceEntry.Choices[0];
        await ((CommunityToolkit.Mvvm.Input.IAsyncRelayCommand)first.SelectChoiceCommand).ExecuteAsync(first);

        // Wait for repository UpdateCurrentNodeId log entry whose parsed scopes include WorkflowId and Operation
        var log = await _fixture.LoggerProvider.WaitForEntryAsync(e =>
        {
            return e.ScopesParsed.Any(s => ScopeValidator.ContainsKeys((IDictionary<string, object?>)s, "WorkflowId", "Operation")
                                           && s["Operation"]?.ToString() == "UpdateCurrentNodeId"
                                           && s["WorkflowId"]?.ToString() == def.Id);
        }, timeoutMs: 2000);

        Assert.NotNull(log);

        // Now chat should have two entries (choice + resulting text)
        Assert.Equal(2, chatList.Entries.Count);

        var persisted = await repo.GetByIdAsync(def.Id);
        Assert.NotNull(persisted);
        Assert.False(string.IsNullOrWhiteSpace(persisted!.CurrentNodeId));

        // Assert we logged creation and updated current node with parsed scopes
        var createdLog = await _fixture.LoggerProvider.WaitForEntryAsync(e => e.ScopesParsed.Any(s => ScopeValidator.ContainsKeys((IDictionary<string, object?>)s, "Operation") && (s["Operation"]?.ToString() == "PersistDefinition" || s["Operation"]?.ToString() == "CreateWorkflow")));
        Assert.NotNull(createdLog);

        var updatedLog = await _fixture.LoggerProvider.WaitForEntryAsync(e => e.ScopesParsed.Any(s => ScopeValidator.ContainsKeys((IDictionary<string, object?>)s, "Operation") && s["Operation"]?.ToString() == "UpdateCurrentNodeId"));
        Assert.NotNull(updatedLog);

        // Validate structured scope for UpdateCurrentNodeId contains WorkflowId
        var updateEntry = updatedLog!;
        ScopeValidator.EnsureAnyScopeContainsKeys(updateEntry.ScopesParsed.Cast<IDictionary<string, object?>>(), "WorkflowId");
        var scopeDict = updateEntry.ScopesParsed.FirstOrDefault(d => d.ContainsKey("WorkflowId"));
        Assert.NotNull(scopeDict);
        Assert.Equal(def.Id, scopeDict!["WorkflowId"]?.ToString());
    }

    [Fact]
    public async Task Integration_LongPath_WithLoop_ExecutesAndPersists()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddProvider(_fixture.LoggerProvider));
        services.AddDbContext<NotesDbContext>(options => options.UseSqlite(_fixture.Connection));
        services.AddScoped<IWorkflowRepository, EfWorkflowRepository>();

        // Register workflow and chat services using extension methods
        services.AddWorkflowServices();
        services.AddChatServices();

        // Register location services with in-memory config (not database for tests)
        services.AddLocationServicesWithInMemoryConfig(options =>
        {
            options.DefaultRadiusMeters = 1000;
            options.MaxRadiusMeters = 5000;
            options.MinRadiusMeters = 50;
        });

        var sp = services.BuildServiceProvider();

        var wfSvc = sp.GetRequiredService<IWorkflowService>();
        var chatSvc = sp.GetRequiredService<ChatService>();
        var chatList = sp.GetRequiredService<ChatListViewModel>();
        var repo = sp.GetRequiredService<IWorkflowRepository>();

        // Workflow with a loop and join
        var plant = @"@startuml
[*] --> Start
:Start;
repeat
:Work;
repeat while (notDone)
if (done) then (yes)
:Finish;
else (no)
:More;
endif
:Finish --> End
@enduml";

        var def = await wfSvc.ImportWorkflowAsync(plant, "wf_long", "LongPath");

        await chatSvc.RenderWorkflowStateAsync(def.Id);

        // First is likely a choice (loop/continue), pick first if choice present
        if (chatList.Entries.Last() is FWH.Common.Chat.ViewModels.ChoiceChatEntry cce)
        {
            var first = cce.Choices[0];
            await ((CommunityToolkit.Mvvm.Input.IAsyncRelayCommand)first.SelectChoiceCommand).ExecuteAsync(first);
            var update = await _fixture.LoggerProvider.WaitForEntryAsync(e => e.ScopesParsed.Any(s => ScopeValidator.ContainsKeys((IDictionary<string, object?>)s, "Operation") && s["Operation"]?.ToString() == "UpdateCurrentNodeId"));
            Assert.NotNull(update);

            // Validate scope contains the workflow id
            Assert.Contains(update.ScopesParsed, d => d.ContainsKey("WorkflowId") && d["WorkflowId"]?.ToString() == def.Id);
        }

        // Ensure we have persisted state
        var persisted = await repo.GetByIdAsync(def.Id);
        Assert.NotNull(persisted);
        Assert.False(string.IsNullOrWhiteSpace(persisted!.CurrentNodeId));
    }
}
