namespace FWH.Common.Chat.ViewModels;

public class TextChatEntry : ChatEntry<TextPayload>
{
    public TextChatEntry(ChatAuthors author, string message)
        : base(author, new TextPayload(message))
    {
    }

    public string Text => Payload.Text;
}
