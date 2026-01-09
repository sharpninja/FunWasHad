using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using FWH.Mobile.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using FWH.Common.Chat.ViewModels;
using FWH.Common.Chat;
using FWH.Common.Chat.Services;
using FWH.Mobile.Services;
using System;
using System.Diagnostics;

namespace FWH.Mobile;

public partial class ChatInputControl : UserControl
{
    private readonly ChatService? _chatService;
    private readonly INotificationService? _notificationService;

    public ChatInputControl()
    {
        InitializeComponent();
        
        var chatInput = App.ServiceProvider.GetRequiredService<FWH.Common.Chat.ViewModels.ChatInputViewModel>();
        _chatService = App.ServiceProvider.GetService<ChatService>();
        _notificationService = App.ServiceProvider.GetService<INotificationService>();
        
        DataContext = chatInput;
        
        // Wire up camera event handler
        chatInput.CameraRequested += OnCameraRequested;
        chatInput.ImageCaptured += OnImageCaptured;
    }

    private async void OnCameraRequested(object? sender, EventArgs e)
    {
        try
        {
            // Get the camera service and capture a photo
            var cameraService = App.ServiceProvider.GetService<FWH.Common.Chat.Services.ICameraService>();
            if (cameraService == null)
            {
                ShowCameraError("Camera service not available");
                return;
            }

            var imageBytes = await cameraService.TakePhotoAsync();
            
            if (imageBytes != null && imageBytes.Length > 0)
            {
                // Update the ImagePayload with the captured image
                var chatInput = DataContext as ChatInputViewModel;
                if (chatInput?.CurrentImage != null)
                {
                    chatInput.CurrentImage.Image = imageBytes;
                    
                    // Raise event to notify that image was captured
                    chatInput.RaiseImageCaptured(imageBytes);
                }
            }
            else
            {
                // Camera could not capture image - show notification
                ShowCameraError("Camera could not be opened. Please try again or check camera permissions.");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error capturing photo: {ex}");
            ShowCameraError($"Camera error: {ex.Message}");
        }
    }

    private void ShowCameraError(string message)
    {
        // Use notification service to show error
        // This will display in chat UI with emoji prefix and log to debug output
        _notificationService?.ShowError(message, "Camera Error");
    }

    private async void OnImageCaptured(object? sender, byte[] imageBytes)
    {
        try
        {
            // Image was captured, advance the workflow
            // For camera nodes, we treat them as non-choice nodes that auto-advance
            var chatInput = DataContext as ChatInputViewModel;
            if (chatInput != null)
            {
                // Clear the input mode back to text
                chatInput.ClearInput();
            }

            // The ChatService should handle advancing the workflow
            // This is handled by the OnImageCaptured in ChatService
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error after image capture: {ex}");
        }
    }
}
