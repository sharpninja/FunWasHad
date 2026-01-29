using FWH.Common.Chat.ViewModels;
using FWH.Common.Workflow;

namespace FWH.Common.Chat.Conversion;

/// <summary>
/// Converts workflow state payloads to chat entries.
/// Single Responsibility: Workflow-to-chat data transformation.
/// </summary>
public class WorkflowToChatConverter : IWorkflowToChatConverter
{
    public IChatEntry<IPayload> ConvertToEntry(WorkflowStatePayload payload, string workflowId)
    {
        ArgumentNullException.ThrowIfNull(payload);

        // Check if this is a camera node
        if (!string.IsNullOrWhiteSpace(payload.NodeLabel) &&
            payload.NodeLabel.Equals("camera", StringComparison.OrdinalIgnoreCase))
        {
            var imagePayload = new ImagePayload
            {
                Image = null, // Will be populated by camera capture
                ShowBorder = true
            };
            return new ImageChatEntry(ChatAuthors.Bot, imagePayload);
        }

        if (!payload.IsChoice)
        {
            return new TextChatEntry(ChatAuthors.Bot, payload.Text ?? string.Empty);
        }

        // Build choice items from workflow options
        var items = payload.Choices.Select((opt, i) =>
            new ChoicesItem(i, opt.DisplayText, opt.TargetNodeId)).ToList();

        var choicePayload = new ChoicePayload(items)
        {
            Prompt = payload.Text ?? "Choose an option",
            Title = ""
        };

        return new ChoiceChatEntry(ChatAuthors.Bot, choicePayload);
    }
}
