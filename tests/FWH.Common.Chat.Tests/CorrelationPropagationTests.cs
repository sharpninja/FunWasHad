using FWH.Common.Chat.Tests.TestFixtures;
using FWH.Common.Chat.ViewModels;
using FWH.Common.Workflow;
using FWH.Mobile.Data.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FWH.Common.Chat.Tests;

public class CorrelationPropagationTests : IClassFixture<SqliteTestFixture>
{
    private readonly SqliteTestFixture _fixture;
    public CorrelationPropagationTests(SqliteTestFixture fixture) => _fixture = fixture;

    /// <summary>
    /// Tests that CorrelationId is correctly propagated from ChatService through the workflow execution pipeline to repository operations in logging scopes.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The logging scope propagation mechanism's ability to carry CorrelationId through the entire workflow execution flow from ChatService to repository updates.</para>
    /// <para><strong>Data involved:</strong> A workflow with branching node A, a custom CorrelationId (GUID string), userId="user-123", tenantId="tenant-a". The workflow is rendered, a choice is selected, triggering advancement and repository updates. A logger provider captures log entries with their scopes.</para>
    /// <para><strong>Why the data matters:</strong> Correlation IDs enable tracing requests across distributed systems and correlating logs from different components. When a user interaction triggers workflow execution, the correlation ID should flow through all operations (controller, executor, repository) so logs can be filtered by correlation ID to see the complete execution trace. This is critical for debugging and monitoring in production systems.</para>
    /// <para><strong>Expected outcome:</strong> After selecting a choice and triggering workflow advancement, a log entry should be captured that contains a scope with CorrelationId matching the provided GUID, and WorkflowId matching the workflow definition ID.</para>
    /// <para><strong>Reason for expectation:</strong> The ChatService should create a logging scope with the provided CorrelationId when RenderWorkflowStateAsync is called. This scope should propagate through all subsequent operations (workflow controller, action executor, repository). The logger provider should capture log entries with the CorrelationId scope intact. The presence of both CorrelationId and WorkflowId in the scopes confirms that correlation tracking works correctly, enabling end-to-end request tracing across the workflow execution pipeline.</para>
    /// </remarks>
    [Fact]
    public async Task CorrelationIdPropagatesFromChatToRepository()
    {
        var sp = _fixture.CreateServiceProvider(services =>
        {
            services.AddScoped<IWorkflowRepository, EfWorkflowRepository>();
            services.AddSingleton<IWorkflowService, WorkflowService>();
            services.AddSingleton<ChatListViewModel>();
            services.AddSingleton<ChatInputViewModel>(sp2 => new ChatInputViewModel(sp2.GetRequiredService<ChatListViewModel>()));
            services.AddSingleton<ChatViewModel>();
            services.AddSingleton<ChatService>();
        });

        var wfSvc = sp.GetRequiredService<IWorkflowService>();
        var chatSvc = sp.GetRequiredService<ChatService>();
        var chatList = sp.GetRequiredService<ChatListViewModel>();

        var plant = "@startuml\n[*] --> A\n:A;\nA --> B\nA --> C\n@enduml";
        var def = await wfSvc.ImportWorkflowAsync(plant, "wf_corr", "CorrTest").ConfigureAwait(true);

        // pick a custom correlation id
        var correlationId = System.Guid.NewGuid().ToString();

        await chatSvc.RenderWorkflowStateAsync(def.Id, userId: "user-123", tenantId: "tenant-a", correlationId: correlationId).ConfigureAwait(true);

        // after initial render we should have a choice entry
        var choiceEntry = chatList.Entries.Last() as ChoiceChatEntry;
        Assert.NotNull(choiceEntry);

        // select first choice to cause an advance and repository update
        var first = choiceEntry!.Choices[0];
        await ((CommunityToolkit.Mvvm.Input.IAsyncRelayCommand)first.SelectChoiceCommand).ExecuteAsync(first).ConfigureAwait(true);

        // wait for repository update log with matching correlation id
        var log = await _fixture.LoggerProvider.WaitForEntryAsync(e => e.ScopesParsed.Any(d => d.ContainsKey("CorrelationId") && d["CorrelationId"]?.ToString() == correlationId)).ConfigureAwait(true);
        Assert.NotNull(log);

        // ensure the workflow id scope is present as well
        Assert.Contains(log!.ScopesParsed, d => d.ContainsKey("WorkflowId") && d["WorkflowId"]?.ToString() == def.Id);
    }
}
