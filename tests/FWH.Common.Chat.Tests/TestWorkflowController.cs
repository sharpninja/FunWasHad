using FWH.Common.Workflow;
using FWH.Common.Workflow.Models;

namespace FWH.Common.Chat.Tests;

class TestWorkflowController : IWorkflowController
{
    private readonly Queue<WorkflowStatePayload> _responses = new();
    public void EnqueueResponse(WorkflowStatePayload p) => _responses.Enqueue(p);
    public Task<WorkflowDefinition> ImportWorkflowAsync(string plantUmlText, string? id = null, string? name = null) => Task.FromResult<WorkflowDefinition>(null!);
    public Task StartInstanceAsync(string workflowId) => Task.CompletedTask;
    public Task RestartInstanceAsync(string workflowId) => Task.CompletedTask;
    public Task<WorkflowStatePayload> GetCurrentStatePayloadAsync(string workflowId) => Task.FromResult(_responses.Count > 0 ? _responses.Dequeue() : new WorkflowStatePayload(false, "", System.Array.Empty<WorkflowChoiceOption>()));
    public Task<bool> AdvanceByChoiceValueAsync(string workflowId, object? choiceValue) => Task.FromResult(true);
    public string? GetCurrentNodeId(string workflowId) => null;
    public bool WorkflowExists(string workflowId) => true;
}
