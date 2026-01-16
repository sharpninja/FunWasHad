using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Provider;
using AndroidX.Core.Content;
using FWH.Common.Chat.Services;
using System;
using System.IO;
using System.Threading.Tasks;
using Android.Content.PM;

namespace FWH.Mobile.Droid.Services;

/// <summary>
/// Android implementation of camera service using MediaStore
/// </summary>
public class AndroidCameraService : ICameraService
{
    private Activity Activity => Platform.CurrentActivity ?? throw new InvalidOperationException("Current activity not available");
    
    private TaskCompletionSource<byte[]?>? _photoTcs;
    private const int CameraRequestCode = 1001;

    public AndroidCameraService()
    {
        // Don't access Platform.CurrentActivity in constructor
        // It will be accessed lazily when methods/properties are called
    }

    public bool IsCameraAvailable
    {
        get
        {
            try
            {
                var activity = Activity;
                var packageManager = activity.PackageManager;
                return packageManager?.HasSystemFeature(PackageManager.FeatureCamera) == true ||
                       packageManager?.HasSystemFeature(PackageManager.FeatureCameraAny) == true;
            }
            catch (InvalidOperationException)
            {
                // Activity not available yet, camera not available
                return false;
            }
        }
    }

    public Task<byte[]?> TakePhotoAsync()
    {
        if (!IsCameraAvailable)
        {
            return Task.FromResult<byte[]?>(null);
        }

        var activity = Activity;
        _photoTcs = new TaskCompletionSource<byte[]?>();

        var intent = new Intent(MediaStore.ActionImageCapture);
        
        if (intent.ResolveActivity(activity.PackageManager) != null)
        {
            activity.StartActivityForResult(intent, CameraRequestCode);
        }
        else
        {
            _photoTcs.TrySetResult(null);
        }

        return _photoTcs.Task;
    }

    /// <summary>
    /// Call this method from MainActivity.OnActivityResult
    /// </summary>
    public void OnActivityResult(int requestCode, Result resultCode, Intent? data)
    {
        if (requestCode == CameraRequestCode && _photoTcs != null)
        {
            if (resultCode == Result.Ok && data?.Extras != null)
            {
                try
                {
                    // Get the thumbnail bitmap
                    var bitmap = (Bitmap?)data.Extras.Get("data");
                    
                    if (bitmap != null)
                    {
                        using var stream = new MemoryStream();
                        bitmap.Compress(Bitmap.CompressFormat.Jpeg, 90, stream);
                        var bytes = stream.ToArray();
                        _photoTcs.TrySetResult(bytes);
                    }
                    else
                    {
                        _photoTcs.TrySetResult(null);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error processing camera result: {ex}");
                    _photoTcs.TrySetResult(null);
                }
            }
            else
            {
                _photoTcs.TrySetResult(null);
            }

            _photoTcs = null;
        }
    }
}

/// <summary>
/// Static helper to access the camera service instance
/// </summary>
public static class Platform
{
    public static Activity? CurrentActivity { get; set; }
}
