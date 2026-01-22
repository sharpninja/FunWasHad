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
