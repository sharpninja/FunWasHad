using Foundation;
using FWH.Common.Chat.Services;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;
using UIKit;

namespace FWH.Mobile.iOS.Services;

/// <summary>
/// iOS implementation of camera service using UIImagePickerController
/// </summary>
public class iOSCameraService : ICameraService
{
    private readonly ILogger<iOSCameraService>? _logger;
    private TaskCompletionSource<byte[]?>? _photoTcs;

    public iOSCameraService(ILogger<iOSCameraService>? logger = null)
    {
        _logger = logger;
    }

    public bool IsCameraAvailable => UIImagePickerController.IsSourceTypeAvailable(UIImagePickerControllerSourceType.Camera);

    public Task<byte[]?> TakePhotoAsync()
    {
        if (!IsCameraAvailable)
        {
            return Task.FromResult<byte[]?>(null);
        }

        _photoTcs = new TaskCompletionSource<byte[]?>();

        var picker = new UIImagePickerController
        {
            SourceType = UIImagePickerControllerSourceType.Camera,
            CameraCaptureMode = UIImagePickerControllerCameraCaptureMode.Photo,
            AllowsEditing = false
        };

        picker.FinishedPickingMedia += OnImagePicked;
        picker.Canceled += OnCanceled;

        var viewController = GetCurrentViewController();
        if (viewController != null)
        {
            viewController.PresentViewController(picker, true, null);
        }
        else
        {
            _photoTcs.TrySetResult(null);
        }

        return _photoTcs.Task;
    }

    private void OnImagePicked(object? sender, UIImagePickerMediaPickedEventArgs e)
    {
        if (sender is UIImagePickerController picker)
        {
            try
            {
                var image = e.OriginalImage;
                if (image != null)
                {
                    using var jpegData = image.AsJPEG(0.9f);
                    var bytes = jpegData.ToArray();
                    _photoTcs?.TrySetResult(bytes);
                }
                else
                {
                    _photoTcs?.TrySetResult(null);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error processing camera result");
                _photoTcs?.TrySetResult(null);
            }
            finally
            {
                picker.DismissViewController(true, null);
                picker.FinishedPickingMedia -= OnImagePicked;
                picker.Canceled -= OnCanceled;
                _photoTcs = null;
            }
        }
    }

    private void OnCanceled(object? sender, EventArgs e)
    {
        if (sender is UIImagePickerController picker)
        {
            _photoTcs?.TrySetResult(null);
            picker.DismissViewController(true, null);
            picker.FinishedPickingMedia -= OnImagePicked;
            picker.Canceled -= OnCanceled;
            _photoTcs = null;
        }
    }

    private UIViewController? GetCurrentViewController()
    {
        var window = UIApplication.SharedApplication.KeyWindow;
        var viewController = window?.RootViewController;

        while (viewController?.PresentedViewController != null)
        {
            viewController = viewController.PresentedViewController;
        }

        return viewController;
    }
}
