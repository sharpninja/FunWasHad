using System;

namespace FWH.Common.Chat.ViewModels;

public enum ChatAuthors
{
    User,
    Bot
}

public interface IChatEntry<out TPayload> where TPayload : IPayload
{
    ChatAuthors Author { get; }
    TPayload Payload { get; }
}
