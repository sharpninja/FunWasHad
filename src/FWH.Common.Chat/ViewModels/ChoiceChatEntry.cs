using System;
using System.Collections.ObjectModel;

namespace FWH.Common.Chat.ViewModels;

public class ChoiceChatEntry : ChatEntry<ChoicePayload>
{
    public ChoiceChatEntry(ChatAuthors author, ChoicePayload payload)
        : base(author, payload ?? throw new ArgumentNullException(nameof(payload)))
    {
    }

    public string Prompt => Payload.Prompt;
    public string Title => Payload.Title;
    public ObservableCollection<ChoicesItem> Choices => Payload.Choices;
    public ChoicesItem? SelectedChoice => Payload.SelectedChoice;
}
