using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FWH.Common.Chat.ViewModels;

public partial class ChatEntry<TPayload> : ObservableObject, IChatEntry<TPayload>
    where TPayload : IPayload
{
    [ObservableProperty]
    private ChatAuthors author;

    [ObservableProperty]
    private TPayload payload;

    public ChatEntry(ChatAuthors author, TPayload payload)
    {
        Author = author;
        Payload = payload;
    }
}

public class ImageChatEntry : ChatEntry<ImagePayload>
{
    public ImageChatEntry(ChatAuthors author, ImagePayload payload)
        : base(author, payload)
    {
    }
    public string? Image => Payload.Image; // neutral
    public bool ShowBorder => Payload.ShowBorder;
}

public class ChoiceChatEntry : ChatEntry<ChoicePayload>
{
    public ChoiceChatEntry(ChatAuthors author, ChoicePayload payload)
        : base(author, payload)
    {
    }

    public string Prompt => Payload.Prompt;
    public string Title => Payload.Title;
    public ObservableCollection<ChoicesItem> Choices => Payload.Choices;
    public ChoicesItem? SelectedChoice => Payload.SelectedChoice;
}

public class TextChatEntry : ChatEntry<TextPayload>
{
    public TextChatEntry(ChatAuthors author, string message)
        : base(author, new TextPayload(message))
    {
    }

    public string Text => Payload.Text;
}
