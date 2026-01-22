namespace FWH.Common.Chat.ViewModels;

public interface IChatEntry<out TPayload> where TPayload : IPayload
{
    ChatAuthors Author { get; }
    TPayload Payload { get; }
}
