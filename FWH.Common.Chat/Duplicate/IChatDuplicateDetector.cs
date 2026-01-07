using FWH.Common.Chat.ViewModels;

namespace FWH.Common.Chat.Duplicate;

/// <summary>
/// Responsible for detecting duplicate chat entries.
/// Single Responsibility: Duplicate detection logic.
/// </summary>
public interface IChatDuplicateDetector
{
    /// <summary>
    /// Determine if a new entry is a duplicate of the last entry.
    /// </summary>
    /// <param name="newEntry">The entry to check</param>
    /// <param name="lastEntry">The previous entry to compare against</param>
    /// <returns>True if the new entry is a duplicate</returns>
    bool IsDuplicate(IChatEntry<IPayload> newEntry, IChatEntry<IPayload>? lastEntry);
}
