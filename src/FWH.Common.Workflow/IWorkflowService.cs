using System.Threading.Tasks;
using FWH.Common.Workflow.Models;

namespace FWH.Common.Workflow;

public interface IWorkflowService
{
    Task<WorkflowDefinition> ImportWorkflowAsync(string plantUmlText, string? id = null, string? name = null);
    Task StartInstanceAsync(string workflowId);
    Task RestartInstanceAsync(string workflowId);
    Task<WorkflowStatePayload> GetCurrentStatePayloadAsync(string workflowId);
    Task<bool> AdvanceByChoiceValueAsync(string workflowId, object? choiceValue);
}
