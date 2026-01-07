using FWH.Common.Chat.ViewModels;

namespace FWH.Common.Chat.Duplicate;

/// <summary>
/// Detects duplicate chat entries to prevent redundant UI elements.
/// Single Responsibility: Duplicate detection logic for chat entries.
/// </summary>
public class ChatDuplicateDetector : IChatDuplicateDetector
{
    public bool IsDuplicate(IChatEntry<IPayload> newEntry, IChatEntry<IPayload>? lastEntry)
    {
        if (newEntry == null || lastEntry == null)
            return false;

        // Only check for choice duplicates
        if (newEntry.Payload.PayloadType != PayloadTypes.Choice)
            return false;

        if (lastEntry.Payload.PayloadType != PayloadTypes.Choice)
            return false;

        var lastChoice = lastEntry.Payload as ChoicePayload;
        var newChoice = newEntry.Payload as ChoicePayload;

        if (lastChoice == null || newChoice == null)
            return false;

        if (lastChoice.Choices.Count != newChoice.Choices.Count)
            return false;

        // Compare choice texts
        for (int i = 0; i < lastChoice.Choices.Count; i++)
        {
            if (lastChoice.Choices[i].ChoiceText != newChoice.Choices[i].ChoiceText)
                return false;
        }

        return true;
    }
}
