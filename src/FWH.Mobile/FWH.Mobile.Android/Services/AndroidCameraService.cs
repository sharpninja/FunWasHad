using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Provider;
using Android.Util;
using AndroidX.Core.Content;
using FWH.Common.Chat.Services;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;
using Android.Content.PM;
using Java.IO;
using JFile = Java.IO.File;

namespace FWH.Mobile.Droid.Services;

/// <summary>
/// Android implementation of camera service using MediaStore and FileProvider (EXTRA_OUTPUT).
/// </summary>
public class AndroidCameraService : ICameraService
{
    private const string FileProviderAuthority = "app.funwashad.fileprovider";

    private Activity? GetActivity()
    {
        try
        {
            return Platform.CurrentActivity;
        }
        catch
        {
            return null;
        }
    }

    private readonly ILogger<AndroidCameraService>? _logger;
    private TaskCompletionSource<byte[]?>? _photoTcs;
    private JFile? _currentPhotoFile;
    private const int CameraRequestCode = 1001;

    public AndroidCameraService(ILogger<AndroidCameraService>? logger = null)
    {
        // Don't access Platform.CurrentActivity in constructor
        // It will be accessed lazily when methods/properties are called
        _logger = logger;
    }

    public bool IsCameraAvailable
    {
        get
        {
            try
            {
                var activity = GetActivity();
                if (activity == null)
                {
                    _logger?.LogWarning("IsCameraAvailable: Activity not available");
                    return false;
                }

                var packageManager = activity.PackageManager;

                if (packageManager == null)
                {
                    _logger?.LogWarning("IsCameraAvailable: PackageManager is null");
                    return false;
                }

                var hasCamera = packageManager.HasSystemFeature(PackageManager.FeatureCamera);
                var hasCameraAny = packageManager.HasSystemFeature(PackageManager.FeatureCameraAny);
                var hardwareAvailable = hasCamera || hasCameraAny;

                // Check camera permission
                var permission = ContextCompat.CheckSelfPermission(activity, global::Android.Manifest.Permission.Camera);
                var permissionGranted = permission == Permission.Granted;

                var isAvailable = hardwareAvailable && permissionGranted;

                _logger?.LogDebug("IsCameraAvailable: HasCamera={HasCamera}, HasCameraAny={HasCameraAny}, PermissionGranted={PermissionGranted}, Result={Result}",
                    hasCamera, hasCameraAny, permissionGranted, isAvailable);

                if (hardwareAvailable && !permissionGranted)
                {
                    _logger?.LogWarning("IsCameraAvailable: Camera hardware available but permission not granted (Status={Status})", permission);
                }

                return isAvailable;
            }
            catch (InvalidOperationException ex)
            {
                // Activity not available yet, camera not available
                _logger?.LogWarning(ex, "IsCameraAvailable: Activity not available");
                return false;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "IsCameraAvailable: Unexpected error");
                return false;
            }
        }
    }

    public Task<byte[]?> TakePhotoAsync()
    {
        const string TAG = "FWH_CAMERA";
        Log.Error(TAG, "TakePhotoAsync: ENTRY"); // High visibility for logcat diagnosis

        if (!IsCameraAvailable)
        {
            Log.Warn(TAG, "TakePhotoAsync: Camera not available - IsCameraAvailable returned false");
            _logger?.LogWarning("Camera not available - IsCameraAvailable returned false");
            return Task.FromResult<byte[]?>(null);
        }

        Task<byte[]?>? photoTask = null;
        try
        {
            var activity = GetActivity();
            if (activity == null)
            {
                Log.Error(TAG, "TakePhotoAsync: Activity not available");
                _logger?.LogError("TakePhotoAsync: Activity not available");
                return Task.FromResult<byte[]?>(null);
            }

            Log.Info(TAG, "TakePhotoAsync: Activity retrieved, creating TaskCompletionSource");
            _photoTcs = new TaskCompletionSource<byte[]?>();
            photoTask = _photoTcs.Task;

            // Create temp file in cache for full-resolution output (EXTRA_OUTPUT). Many devices
            // return null or thumbnail-only without EXTRA_OUTPUT; FileProvider required on Android 7+.
            var cacheDir = activity.CacheDir ?? throw new InvalidOperationException("CacheDir is null");
            var photoFile = new JFile(cacheDir, "photo_" + DateTime.UtcNow.Ticks + ".jpg");
            try
            {
                photoFile.CreateNewFile();
            }
            catch (Java.IO.IOException ioEx)
            {
                Log.Error(TAG, $"TakePhotoAsync: Failed to create temp file: {ioEx.Message}", ioEx);
                _logger?.LogError(ioEx, "Failed to create temp file for camera");
                _photoTcs.TrySetResult(null);
                return photoTask;
            }

            _currentPhotoFile = photoFile;
            global::Android.Net.Uri? photoUri;
            try
            {
                photoUri = FileProvider.GetUriForFile(activity, FileProviderAuthority, photoFile);
            }
            catch (Exception fpEx)
            {
                Log.Error(TAG, $"TakePhotoAsync: FileProvider.GetUriForFile failed: {fpEx.Message}", fpEx);
                _logger?.LogError(fpEx, "FileProvider.GetUriForFile failed");
                _currentPhotoFile = null;
                TryDelete(photoFile);
                _photoTcs.TrySetResult(null);
                return photoTask;
            }

            if (photoUri == null)
            {
                CleanupAndComplete(null);
                return photoTask;
            }

            var intent = new Intent(MediaStore.ActionImageCapture);
            intent.PutExtra(MediaStore.ExtraOutput, photoUri);
            intent.AddFlags(ActivityFlags.GrantWriteUriPermission | ActivityFlags.GrantReadUriPermission);

            var packageManager = activity.PackageManager;
            if (packageManager == null)
            {
                Log.Error(TAG, "TakePhotoAsync: PackageManager is null - cannot resolve camera intent");
                _logger?.LogError("PackageManager is null - cannot resolve camera intent");
                CleanupAndComplete(null);
                return photoTask;
            }

            var resolvedActivity = intent.ResolveActivity(packageManager);
            if (resolvedActivity == null)
            {
                Log.Error(TAG, "TakePhotoAsync: Camera intent could not be resolved - no camera app available or permission denied");
                _logger?.LogError("Camera intent could not be resolved - no camera app available or permission denied");
                CleanupAndComplete(null);
                return photoTask;
            }

            // Grant URI permissions to camera app(s) that can handle the intent
            var resolveInfoList = packageManager.QueryIntentActivities(intent, PackageInfoFlags.MatchDefaultOnly);
            foreach (var resolveInfo in resolveInfoList)
            {
                var packageName = resolveInfo.ActivityInfo?.PackageName;
                if (!string.IsNullOrEmpty(packageName))
                    activity.GrantUriPermission(packageName, photoUri, ActivityFlags.GrantWriteUriPermission | ActivityFlags.GrantReadUriPermission);
            }

            Log.Info(TAG, $"TakePhotoAsync: Starting camera activity: {resolvedActivity.ClassName}, RequestCode={CameraRequestCode}");
            _logger?.LogInformation("Starting camera activity: {ActivityName}, RequestCode={RequestCode}",
                resolvedActivity.ClassName, CameraRequestCode);

            try
            {
                activity.StartActivityForResult(intent, CameraRequestCode);
                Log.Info(TAG, "TakePhotoAsync: Camera activity started successfully");
                _logger?.LogDebug("Camera activity started successfully");
            }
            catch (Exception startEx)
            {
                Log.Error(TAG, $"TakePhotoAsync: Failed to start camera activity: {startEx.Message}", startEx);
                _logger?.LogError(startEx, "Failed to start camera activity");
                CleanupAndComplete(null);
                return photoTask;
            }
        }
        catch (Exception ex)
        {
            Log.Error(TAG, $"TakePhotoAsync: Exception while starting camera: {ex.Message}", ex);
            _logger?.LogError(ex, "Exception while starting camera");
            var t = _photoTcs?.Task;
            CleanupAndComplete(null);
            return t ?? Task.FromResult<byte[]?>(null);
        }

        return photoTask;
    }

    private void CleanupAndComplete(byte[]? bytes)
    {
        TryDelete(_currentPhotoFile);
        _currentPhotoFile = null;
        _photoTcs?.TrySetResult(bytes);
        _photoTcs = null;
    }

    private static void TryDelete(Java.IO.File? file)
    {
        try
        {
            if (file != null && file.Exists())
                file.Delete();
        }
        catch
        {
            // Ignore
        }
    }

    /// <summary>
    /// Call this method from MainActivity.OnActivityResult
    /// </summary>
    public void OnActivityResult(int requestCode, Result resultCode, Intent? data)
    {
        const string TAG = "FWH_CAMERA";
        Log.Error(TAG, $"OnActivityResult: RequestCode={requestCode}, ExpectedCode={CameraRequestCode}, ResultCode={resultCode}, HasTaskCompletionSource={_photoTcs != null}");
        _logger?.LogDebug("OnActivityResult called: RequestCode={RequestCode}, ExpectedCode={ExpectedCode}, ResultCode={ResultCode}, HasTaskCompletionSource={HasTaskCompletionSource}",
            requestCode, CameraRequestCode, resultCode, _photoTcs != null);

        if (requestCode != CameraRequestCode || _photoTcs == null)
            return;

        if (resultCode != Result.Ok)
        {
            _logger?.LogWarning("Camera activity returned non-OK result: {ResultCode}", resultCode);
            CleanupAndComplete(null);
            return;
        }

        try
        {
            // Prefer full-resolution file from EXTRA_OUTPUT; fallback to thumbnail "data" extra.
            byte[]? bytes = null;
            if (_currentPhotoFile != null && _currentPhotoFile.Exists() && _currentPhotoFile.Length() > 0)
            {
                bytes = global::System.IO.File.ReadAllBytes(_currentPhotoFile.AbsolutePath);
                Log.Info(TAG, $"OnActivityResult: Read {bytes?.Length ?? 0} bytes from EXTRA_OUTPUT file");
            }
            else if (data?.Extras != null)
            {
                var bitmap = (Bitmap?)data.Extras.Get("data");
                if (bitmap != null)
                {
                    using var stream = new MemoryStream();
                    bitmap.Compress(Bitmap.CompressFormat.Jpeg!, 90, stream);
                    bytes = stream.ToArray();
                    Log.Info(TAG, $"OnActivityResult: Using thumbnail from data extra, {bytes?.Length ?? 0} bytes");
                }
            }

            CleanupAndComplete(bytes);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error processing camera result");
            CleanupAndComplete(null);
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
