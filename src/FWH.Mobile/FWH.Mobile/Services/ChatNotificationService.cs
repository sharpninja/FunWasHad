using System;
using FWH.Common.Chat.ViewModels;
using Microsoft.Extensions.Logging;

namespace FWH.Mobile.Services;

/// <summary>
/// Simple notification service that uses the chat interface for user notifications.
/// Logging is used as a secondary channel for development/troubleshooting.
/// </summary>
public class ChatNotificationService : INotificationService
{
    private readonly ChatListViewModel _chatList;
    private readonly ILogger<ChatNotificationService>? _logger;

    public ChatNotificationService(ChatListViewModel chatList, ILogger<ChatNotificationService>? logger = null)
    {
        _chatList = chatList ?? throw new ArgumentNullException(nameof(chatList));
        _logger = logger;
    }

    public void ShowError(string message, string? title = null)
    {
        var fullMessage = title != null ? $"{title}: {message}" : message;
        _logger?.LogError("Notification: {Message}", fullMessage);

        _chatList.AddEntry(new TextChatEntry(
            ChatAuthors.Bot,
            $"❌ {fullMessage}"));
    }

    public void ShowSuccess(string message, string? title = null)
    {
        var fullMessage = title != null ? $"{title}: {message}" : message;
        _logger?.LogInformation("Notification: {Message}", fullMessage);

        _chatList.AddEntry(new TextChatEntry(
            ChatAuthors.Bot,
            $"✅ {fullMessage}"));
    }

    public void ShowInfo(string message, string? title = null)
    {
        var fullMessage = title != null ? $"{title}: {message}" : message;
        _logger?.LogInformation("Notification: {Message}", fullMessage);

        _chatList.AddEntry(new TextChatEntry(
            ChatAuthors.Bot,
            $"ℹ️ {fullMessage}"));
    }

    public void ShowWarning(string message, string? title = null)
    {
        var fullMessage = title != null ? $"{title}: {message}" : message;
        _logger?.LogWarning("Notification: {Message}", fullMessage);

        _chatList.AddEntry(new TextChatEntry(
            ChatAuthors.Bot,
            $"⚠️ {fullMessage}"));
    }
}
