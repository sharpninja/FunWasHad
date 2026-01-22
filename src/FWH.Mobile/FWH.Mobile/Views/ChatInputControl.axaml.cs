using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using FWH.Mobile.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using FWH.Common.Chat.ViewModels;
using FWH.Common.Chat;
using FWH.Common.Chat.Services;
using FWH.Mobile.Services;
using Microsoft.Extensions.Logging;
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

        System.Diagnostics.Debug.WriteLine("ChatInputControl: Constructor called");
        var chatInput = App.ServiceProvider.GetRequiredService<FWH.Common.Chat.ViewModels.ChatInputViewModel>();
        _chatService = App.ServiceProvider.GetService<ChatService>();
        _notificationService = App.ServiceProvider.GetService<INotificationService>();

        DataContext = chatInput;

        // Wire up camera event handler
        System.Diagnostics.Debug.WriteLine("ChatInputControl: Wiring up CameraRequested event handler");
        chatInput.CameraRequested += OnCameraRequested;
        chatInput.ImageCaptured += OnImageCaptured;
        System.Diagnostics.Debug.WriteLine("ChatInputControl: Event handlers wired up successfully");
    }

    private async void OnCameraRequested(object? sender, EventArgs e)
    {
        try
        {
            Debug.WriteLine("ChatInputControl: OnCameraRequested called");
            var logger = App.ServiceProvider?.GetService<ILogger<ChatInputControl>>();
            logger?.LogInformation("Camera requested from ChatInputControl");
            Debug.WriteLine("ChatInputControl: Camera requested from ChatInputControl");

            // Get the camera service and capture a photo
            var cameraService = App.ServiceProvider?.GetService<FWH.Common.Chat.Services.ICameraService>();
            if (cameraService == null)
            {
                Debug.WriteLine("ChatInputControl: ERROR - Camera service is null - not registered in DI");
                logger?.LogError("Camera service is null - not registered in DI");
                ShowCameraError("Camera service not available");
                return;
            }

            Debug.WriteLine($"ChatInputControl: Camera service retrieved, type: {cameraService.GetType().Name}");
            logger?.LogDebug("Camera service retrieved, type: {Type}", cameraService.GetType().Name);
            logger?.LogInformation("Calling TakePhotoAsync...");
            Debug.WriteLine("ChatInputControl: Calling TakePhotoAsync...");

            var imageBytes = await cameraService.TakePhotoAsync();

            Debug.WriteLine($"ChatInputControl: TakePhotoAsync completed. ImageBytes length: {imageBytes?.Length ?? 0}");
            logger?.LogInformation("TakePhotoAsync completed. ImageBytes length: {Length}", imageBytes?.Length ?? 0);

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
                logger?.LogWarning("Camera returned null or empty image bytes");
                ShowCameraError("Camera could not be opened. Please try again or check camera permissions.");
            }
        }
        catch (Exception ex)
        {
            var logger = App.ServiceProvider?.GetService<ILogger<ChatInputControl>>();
            logger?.LogError(ex, "Error capturing photo");
            ShowCameraError($"Camera error: {ex.Message}");
        }
    }

    private void ShowCameraError(string message)
    {
        // Use notification service to show error
        // This will display in chat UI with emoji prefix and log to debug output
        _notificationService?.ShowError(message, "Camera Error");
    }

    private void OnImageCaptured(object? sender, byte[] imageBytes)
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
            var logger = App.ServiceProvider?.GetService<ILogger<ChatInputControl>>();
            logger?.LogError(ex, "Error after image capture");
        }
    }
}
