//using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FWH.Common.Chat.Services;
using System;
using System.IO;
using System.Threading.Tasks;

namespace FWH.Mobile.ViewModels;

/// <summary>
/// ViewModel for the camera capture control
/// </summary>
public partial class CameraCaptureViewModel : ObservableObject
{
    private readonly ICameraService _cameraService;

    [ObservableProperty]
    private byte[]? _capturedImage;

    [ObservableProperty]
    private bool _isCameraAvailable;

    [ObservableProperty]
    private bool _hasImage;

    [ObservableProperty]
    private bool _isCapturing;

    [ObservableProperty]
    private string _statusMessage = "Tap to capture photo";

    public CameraCaptureViewModel(ICameraService cameraService)
    {
        _cameraService = cameraService ?? throw new ArgumentNullException(nameof(cameraService));
        IsCameraAvailable = _cameraService.IsCameraAvailable;
    }

    [RelayCommand]
    private async Task CapturePhotoAsync()
    {
        if (IsCapturing || !IsCameraAvailable)
            return;

        try
        {
            IsCapturing = true;
            StatusMessage = "Opening camera...";

            var imageBytes = await _cameraService.TakePhotoAsync();

            if (imageBytes != null && imageBytes.Length > 0)
            {
                CapturedImage = imageBytes;
                HasImage = true;
                StatusMessage = "Photo captured successfully!";
            }
            else
            {
                StatusMessage = "Photo capture cancelled";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"Camera capture error: {ex}");
        }
        finally
        {
            IsCapturing = false;
        }
    }

    [RelayCommand]
    private void ClearPhoto()
    {
        CapturedImage = null;
        HasImage = false;
        StatusMessage = "Tap to capture photo";
    }

    /// <summary>
    /// Gets the captured image bytes (JPEG format)
    /// </summary>
    public byte[]? GetCapturedImageBytes()
    {
        if (CapturedImage == null)
            return null;

        return CapturedImage;
    }
}
