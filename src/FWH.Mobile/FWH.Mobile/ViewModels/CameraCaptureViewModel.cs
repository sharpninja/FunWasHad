//using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FWH.Common.Chat.Services;
using FWH.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace FWH.Mobile.ViewModels;

/// <summary>
/// ViewModel for the camera capture control
/// </summary>
public partial class CameraCaptureViewModel : ObservableObject
{
    private readonly ICameraService _cameraService;
    private readonly ILogger<CameraCaptureViewModel>? _logger;
    private readonly IImageService? _imageService;

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

    public CameraCaptureViewModel(
        ICameraService cameraService,
        ILogger<CameraCaptureViewModel>? logger = null,
        IImageService? imageService = null)
    {
        _cameraService = cameraService ?? throw new ArgumentNullException(nameof(cameraService));
        _logger = logger;
        _imageService = imageService;
        // Defer IsCameraAvailable check until Activity is available
        // Initialize to false - will be updated when actually needed
        IsCameraAvailable = false;
    }

    [RelayCommand]
    private async Task CapturePhotoAsync()
    {
        // Check camera availability when actually needed (Activity should be available by now)
        if (!IsCameraAvailable)
        {
            try
            {
                IsCameraAvailable = _cameraService.IsCameraAvailable;
            }
            catch (InvalidOperationException ex)
            {
                _logger?.LogWarning(ex, "Failed to check camera availability");
                IsCameraAvailable = false;
            }
            catch (PlatformNotSupportedException ex)
            {
                _logger?.LogWarning(ex, "Camera not supported on this platform");
                IsCameraAvailable = false;
            }
        }

        if (IsCapturing || !IsCameraAvailable)
            return;

        try
        {
            IsCapturing = true;
            StatusMessage = "Opening camera...";

            var imageBytes = await _cameraService.TakePhotoAsync().ConfigureAwait(false);

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
        catch (OperationCanceledException ex)
        {
            StatusMessage = "Photo capture cancelled";
            _logger?.LogInformation(ex, "Camera capture canceled");
        }
        catch (InvalidOperationException ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            _logger?.LogError(ex, "Camera capture error");
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
