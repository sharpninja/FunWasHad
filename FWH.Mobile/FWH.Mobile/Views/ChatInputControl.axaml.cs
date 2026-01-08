using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using FWH.Mobile.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using FWH.Common.Chat.ViewModels;
using FWH.Common.Chat;
using FWH.Common.Chat.Services;
using System;

namespace FWH.Mobile;

public partial class ChatInputControl : UserControl
{
    private readonly ChatService? _chatService;

    public ChatInputControl()
    {
        InitializeComponent();
        
        var chatInput = App.ServiceProvider.GetRequiredService<FWH.Common.Chat.ViewModels.ChatInputViewModel>();
        _chatService = App.ServiceProvider.GetService<ChatService>();
        
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
            var cameraService = App.ServiceProvider.GetService<ICameraService>();
            if (cameraService == null)
                return;

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
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error capturing photo: {ex}");
        }
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
            System.Diagnostics.Debug.WriteLine($"Error after image capture: {ex}");
        }
    }
}
