namespace FWH.Common.Chat.ViewModels;

public class ImageChatEntry : ChatEntry<ImagePayload>
{
    public ImageChatEntry(ChatAuthors author, ImagePayload payload)
        : base(author, payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
    }
    public byte[]? Image => Payload.Image;
    public bool ShowBorder => Payload.ShowBorder;
}
