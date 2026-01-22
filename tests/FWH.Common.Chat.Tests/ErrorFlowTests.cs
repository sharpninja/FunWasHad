using System;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using FWH.Common.Chat.Tests.TestFixtures;
using FWH.Mobile.Data.Data;
using FWH.Mobile.Data.Repositories;
using FWH.Mobile.Data.Models;
using FWH.Common.Workflow;
using FWH.Common.Chat.ViewModels;
using FWH.Common.Chat;
using Xunit;

namespace FWH.Common.Chat.Tests;

public class ErrorFlowTests : IClassFixture<SqliteTestFixture>
{
    private readonly SqliteTestFixture _fixture;

    public ErrorFlowTests(SqliteTestFixture fixture)
    {
        _fixture = fixture;
    }

    class ThrowOnUpdateRepository : IWorkflowRepository
    {
        private readonly IWorkflowRepository _inner;
        public ThrowOnUpdateRepository(IWorkflowRepository inner) => _inner = inner;
        public Task<WorkflowDefinitionEntity> CreateAsync(WorkflowDefinitionEntity def, CancellationToken cancellationToken = default) => _inner.CreateAsync(def, cancellationToken);
        public Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default) => _inner.DeleteAsync(id, cancellationToken);
        public Task<IEnumerable<WorkflowDefinitionEntity>> GetAllAsync(CancellationToken cancellationToken = default) => _inner.GetAllAsync(cancellationToken);
        public Task<WorkflowDefinitionEntity?> GetByIdAsync(string id, CancellationToken cancellationToken = default) => _inner.GetByIdAsync(id, cancellationToken);
        public Task<WorkflowDefinitionEntity> UpdateAsync(WorkflowDefinitionEntity def, CancellationToken cancellationToken = default) => _inner.UpdateAsync(def, cancellationToken);
        public Task<IEnumerable<WorkflowDefinitionEntity>> FindByNamePatternAsync(string namePattern, DateTimeOffset since, CancellationToken cancellationToken = default) => _inner.FindByNamePatternAsync(namePattern, since, cancellationToken);
        public Task<bool> UpdateCurrentNodeIdAsync(string workflowDefinitionId, string? currentNodeId, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Simulated persistence failure on update");
        }
    }

    class ThrowOnCreateRepository : IWorkflowRepository
    {
        private readonly IWorkflowRepository _inner;
        public ThrowOnCreateRepository(IWorkflowRepository inner) => _inner = inner;
        public Task<WorkflowDefinitionEntity> CreateAsync(WorkflowDefinitionEntity def, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Simulated persistence failure on create");
        }
        public Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default) => _inner.DeleteAsync(id, cancellationToken);
        public Task<IEnumerable<WorkflowDefinitionEntity>> GetAllAsync(CancellationToken cancellationToken = default) => _inner.GetAllAsync(cancellationToken);
        public Task<WorkflowDefinitionEntity?> GetByIdAsync(string id, CancellationToken cancellationToken = default) => _inner.GetByIdAsync(id, cancellationToken);
        public Task<WorkflowDefinitionEntity> UpdateAsync(WorkflowDefinitionEntity def, CancellationToken cancellationToken = default) => _inner.UpdateAsync(def, cancellationToken);
        public Task<IEnumerable<WorkflowDefinitionEntity>> FindByNamePatternAsync(string namePattern, DateTimeOffset since, CancellationToken cancellationToken = default) => _inner.FindByNamePatternAsync(namePattern, since, cancellationToken);
        public Task<bool> UpdateCurrentNodeIdAsync(string workflowDefinitionId, string? currentNodeId, CancellationToken cancellationToken = default) => _inner.UpdateCurrentNodeIdAsync(workflowDefinitionId, currentNodeId, cancellationToken);
    }

    /// <summary>
    /// Tests that when the workflow repository throws an exception during UpdateCurrentNodeIdAsync, the error is logged with proper scopes (Operation, WorkflowId) and the exception is captured.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The error handling and logging behavior when workflow state persistence fails due to repository exceptions.</para>
    /// <para><strong>Data involved:</strong> A ThrowOnUpdateRepository wrapper that throws InvalidOperationException when UpdateCurrentNodeIdAsync is called. A workflow with branching node A is imported, rendered to chat, and a choice is selected, which triggers workflow advancement and attempts to update the current node ID, causing the exception.</para>
    /// <para><strong>Why the data matters:</strong> Database failures, connection issues, or constraint violations can cause repository operations to throw exceptions. The workflow system must handle these gracefully by logging errors with sufficient context (operation name, workflow ID) for debugging. This test validates that exceptions are caught, logged with proper scopes, and don't crash the application.</para>
    /// <para><strong>Expected outcome:</strong> After selecting a choice, a log entry should be captured with Level=Error or Warning, containing scopes with Operation="UpdateCurrentNodeId" and WorkflowId matching the workflow definition ID, and the Exception property should not be null.</para>
    /// <para><strong>Reason for expectation:</strong> The workflow controller should catch exceptions from UpdateCurrentNodeIdAsync, create a logging scope with Operation="UpdateCurrentNodeId" and WorkflowId, and log the error. The scopes allow filtering logs by operation and workflow, and the exception details enable debugging. The presence of the exception in the log confirms error details are preserved for troubleshooting.</para>
    /// </remarks>
    [Fact]
    public async Task Advance_WhenRepoThrows_UpdateLogsExceptionAndContainsCorrelationAndWorkflowScope()
    {
        var sp = _fixture.CreateServiceProvider(services =>
        {
            services.AddScoped<EfWorkflowRepository>();
            services.AddScoped<IWorkflowRepository>(sp2 => new ThrowOnUpdateRepository(sp2.GetRequiredService<EfWorkflowRepository>()));

            // Note: IWorkflowController, IWorkflowService and other components already registered by fixture
            services.AddSingleton<ChatListViewModel>();
            services.AddSingleton<ChatInputViewModel>(sp2 => new ChatInputViewModel(sp2.GetRequiredService<ChatListViewModel>()));
            services.AddSingleton<ChatViewModel>();
            services.AddSingleton<ChatService>();
        });

        var wfSvc = sp.GetRequiredService<IWorkflowService>();
        var repo = sp.GetRequiredService<IWorkflowRepository>();
        var chatSvc = sp.GetRequiredService<ChatService>();
        var chatList = sp.GetRequiredService<ChatListViewModel>();

        var plant = "@startuml\n[*] --> A\n:A;\nA --> B\nA --> C\n@enduml";
        var def = await wfSvc.ImportWorkflowAsync(plant, "wf_err_update", "ErrUpdate");

        // Render and select first choice which will trigger UpdateCurrentNodeIdAsync that throws
        await chatSvc.RenderWorkflowStateAsync(def.Id);
        var choiceEntry = chatList.Entries.Last() as ChoiceChatEntry;
        Assert.NotNull(choiceEntry);
        var first = choiceEntry!.Choices[0];

        await ((CommunityToolkit.Mvvm.Input.IAsyncRelayCommand)first.SelectChoiceCommand).ExecuteAsync(first);

        // Wait for repository UpdateCurrentNodeId Operation log (warning/error) containing WorkflowId
        var log = await _fixture.LoggerProvider.WaitForEntryAsync(e =>
            (e.Level == LogLevel.Error || e.Level == LogLevel.Warning) && e.ScopesParsed.Any(d => d.ContainsKey("Operation") && d["Operation"]?.ToString() == "UpdateCurrentNodeId" && d.ContainsKey("WorkflowId") && d["WorkflowId"]?.ToString() == def.Id)
        );

        Assert.NotNull(log);
        Assert.True(log!.Level == LogLevel.Warning || log.Level == LogLevel.Error);
        Assert.Contains(log.ScopesParsed, d => d.ContainsKey("Operation") && d["Operation"]?.ToString() == "UpdateCurrentNodeId");
        Assert.NotNull(log.Exception);
    }

    [Fact]
    public async Task Import_WhenRepoThrows_ErrorLoggedWithPersistScope()
    {
        var sp = _fixture.CreateServiceProvider(services =>
        {
            services.AddScoped<EfWorkflowRepository>();
            services.AddScoped<IWorkflowRepository>(sp2 => new ThrowOnCreateRepository(sp2.GetRequiredService<EfWorkflowRepository>()));

            // Note: IWorkflowController and IWorkflowService already registered by fixture
        });

        var wfSvc = sp.GetRequiredService<IWorkflowService>();

        var plant = "@startuml\n[*] --> Start\n:Start;\nStart --> End\n@enduml";

        var def = await wfSvc.ImportWorkflowAsync(plant, "wf_err_create", "ErrCreate");

        // Wait for error log with PersistDefinition scope
        var log = await _fixture.LoggerProvider.WaitForEntryAsync(e =>
            e.Level == LogLevel.Error && e.ScopesParsed.Any(d => d.ContainsKey("Operation") && d["Operation"]?.ToString() == "PersistDefinition" && d.ContainsKey("WorkflowId") && d["WorkflowId"]?.ToString() == def.Id)
        );

        Assert.NotNull(log);
        Assert.Equal(LogLevel.Error, log!.Level);
        Assert.NotNull(log.Exception);
    }
}
