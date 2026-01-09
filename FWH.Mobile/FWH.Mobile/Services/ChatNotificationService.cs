using System;
using System.Diagnostics;
using FWH.Common.Chat.ViewModels;

namespace FWH.Mobile.Services;

/// <summary>
/// Simple notification service that uses the chat interface for user notifications.
/// Debug output is used as a secondary channel for development/troubleshooting.
/// </summary>
public class ChatNotificationService : INotificationService
{
    private readonly ChatListViewModel _chatList;

    public ChatNotificationService(ChatListViewModel chatList)
    {
        _chatList = chatList ?? throw new ArgumentNullException(nameof(chatList));
    }

    public void ShowError(string message, string? title = null)
    {
        var fullMessage = title != null ? $"{title}: {message}" : message;
        Debug.WriteLine($"[ERROR] {fullMessage}");
        
        _chatList.AddEntry(new TextChatEntry(
            ChatAuthors.Bot,
            $"❌ {fullMessage}"));
    }

    public void ShowSuccess(string message, string? title = null)
    {
        var fullMessage = title != null ? $"{title}: {message}" : message;
        Debug.WriteLine($"[SUCCESS] {fullMessage}");
        
        _chatList.AddEntry(new TextChatEntry(
            ChatAuthors.Bot,
            $"✅ {fullMessage}"));
    }

    public void ShowInfo(string message, string? title = null)
    {
        var fullMessage = title != null ? $"{title}: {message}" : message;
        Debug.WriteLine($"[INFO] {fullMessage}");
        
        _chatList.AddEntry(new TextChatEntry(
            ChatAuthors.Bot,
            $"ℹ️ {fullMessage}"));
    }

    public void ShowWarning(string message, string? title = null)
    {
        var fullMessage = title != null ? $"{title}: {message}" : message;
        Debug.WriteLine($"[WARNING] {fullMessage}");
        
        _chatList.AddEntry(new TextChatEntry(
            ChatAuthors.Bot,
            $"⚠️ {fullMessage}"));
    }
}
