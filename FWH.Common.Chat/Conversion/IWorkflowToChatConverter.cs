using FWH.Common.Chat.ViewModels;
using FWH.Common.Workflow;

namespace FWH.Common.Chat.Conversion;

/// <summary>
/// Responsible for converting workflow state payloads to chat entries.
/// Single Responsibility: Transform workflow data into chat UI models.
/// </summary>
public interface IWorkflowToChatConverter
{
    /// <summary>
    /// Convert a workflow state payload into a chat entry.
    /// </summary>
    /// <param name="payload">The workflow state payload to convert</param>
    /// <param name="workflowId">The workflow ID for context</param>
    /// <returns>A chat entry ready for display</returns>
    IChatEntry<IPayload> ConvertToEntry(WorkflowStatePayload payload, string workflowId);
}
