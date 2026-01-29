using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.Provider;
using Android.Util;
using AndroidX.Core.Content;
using FWH.Common.Chat.Services;
using Microsoft.Extensions.Logging;
using JFile = Java.IO.File;

namespace FWH.Mobile.Droid.Services;

/// <summary>
/// Android implementation of camera service using MediaStore and FileProvider (EXTRA_OUTPUT).
/// </summary>
public partial class AndroidCameraService : ICameraService
{
    private const string FileProviderAuthority = "app.funwashad.fileprovider";

    private static Activity? GetActivity()
    {
        try
        {
            return AndroidCameraPlatform.CurrentActivity;
        }
        catch (InvalidOperationException)
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
                    Logger.IsCameraUnavailable(_logger);
                    return false;
                }

                var packageManager = activity.PackageManager;

                if (packageManager == null)
                {
                    Logger.PackageManagerNull(_logger);
                    return false;
                }

                var hasCamera = packageManager.HasSystemFeature(PackageManager.FeatureCamera);
                var hasCameraAny = packageManager.HasSystemFeature(PackageManager.FeatureCameraAny);
                var hardwareAvailable = hasCamera || hasCameraAny;

                // Check camera permission
                var permission = ContextCompat.CheckSelfPermission(activity, global::Android.Manifest.Permission.Camera);
                var permissionGranted = permission == Permission.Granted;

                var isAvailable = hardwareAvailable && permissionGranted;

                Logger.IsCameraAvailableDebug(_logger, hasCamera, hasCameraAny, permissionGranted, isAvailable);

                if (hardwareAvailable && !permissionGranted)
                {
                    Logger.CameraPermissionMissing(_logger, permission);
                }

                return isAvailable;
            }
            catch (InvalidOperationException ex)
            {
                Logger.ActivityNotAvailable(_logger, ex);
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
            Logger.CameraNotAvailable(_logger);
            return Task.FromResult<byte[]?>(null);
        }

        Task<byte[]?>? photoTask = null;
        try
        {
            var activity = GetActivity();
            if (activity == null)
            {
                Log.Error(TAG, "TakePhotoAsync: Activity not available");
                Logger.ActivityNotAvailableError(_logger);
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
                Logger.TempFileCreateFailed(_logger, ioEx);
                _photoTcs.TrySetResult(null);
                photoFile.Dispose();
                return photoTask;
            }

            _currentPhotoFile = photoFile;
            global::Android.Net.Uri? photoUri;
            try
            {
                photoUri = FileProvider.GetUriForFile(activity, FileProviderAuthority, photoFile);
            }
            catch (Java.Lang.IllegalArgumentException fpEx)
            {
                Log.Error(TAG, $"TakePhotoAsync: FileProvider.GetUriForFile failed: {fpEx.Message}", fpEx);
                Logger.FileProviderFailed(_logger, fpEx);
                _currentPhotoFile = null;
                TryDelete(photoFile);
                photoFile.Dispose();
                _photoTcs.TrySetResult(null);
                return photoTask;
            }

            if (photoUri == null)
            {
                CleanupAndComplete(null);
                return photoTask;
            }

            using var intent = new Intent(MediaStore.ActionImageCapture);
            intent.PutExtra(MediaStore.ExtraOutput, photoUri);
            intent.AddFlags(ActivityFlags.GrantWriteUriPermission | ActivityFlags.GrantReadUriPermission);

            var packageManager = activity.PackageManager;
            if (packageManager == null)
            {
                Log.Error(TAG, "TakePhotoAsync: PackageManager is null - cannot resolve camera intent");
                Logger.PackageManagerNull(_logger);
                CleanupAndComplete(null);
                return photoTask;
            }

            var resolvedActivity = intent.ResolveActivity(packageManager);
            if (resolvedActivity == null)
            {
                Log.Error(TAG, "TakePhotoAsync: Camera intent could not be resolved - no camera app available or permission denied");
                Logger.CameraIntentNotResolved(_logger);
                CleanupAndComplete(null);
                return photoTask;
            }

            // Grant URI permissions to camera app(s) that can handle the intent
            var resolveInfoList = packageManager.QueryIntentActivities(intent, PackageInfoFlags.MatchDefaultOnly);
            foreach (var resolveInfo in resolveInfoList)
            {
                var packageName = resolveInfo.ActivityInfo?.PackageName;
                if (!string.IsNullOrEmpty(packageName))
                {
                    activity.GrantUriPermission(packageName, photoUri, ActivityFlags.GrantWriteUriPermission | ActivityFlags.GrantReadUriPermission);
                }
            }

            Log.Info(TAG, $"TakePhotoAsync: Starting camera activity: {resolvedActivity.ClassName}, RequestCode={CameraRequestCode}");
            Logger.StartingCameraActivity(_logger, resolvedActivity.ClassName, CameraRequestCode);

            try
            {
                activity.StartActivityForResult(intent, CameraRequestCode);
                Log.Info(TAG, "TakePhotoAsync: Camera activity started successfully");
                Logger.CameraActivityStarted(_logger);
            }
            catch (ActivityNotFoundException startEx)
            {
                Log.Error(TAG, $"TakePhotoAsync: Failed to start camera activity: {startEx.Message}", startEx);
                Logger.StartCameraFailed(_logger, startEx);
                CleanupAndComplete(null);
                return photoTask;
            }
        }
        catch (InvalidOperationException ex)
        {
            Log.Error(TAG, $"TakePhotoAsync: Invalid operation: {ex.Message}", ex);
            Logger.StartCameraInvalidOperation(_logger, ex);
            var t = _photoTcs?.Task;
            CleanupAndComplete(null);
            return t ?? Task.FromResult<byte[]?>(null);
        }

        return photoTask;
    }

    private void CleanupAndComplete(byte[]? bytes)
    {
        TryDelete(_currentPhotoFile);
        _currentPhotoFile?.Dispose();
        _currentPhotoFile = null;
        _photoTcs?.TrySetResult(bytes);
        _photoTcs = null;
    }

    private static void TryDelete(Java.IO.File? file)
    {
        try
        {
            if (file != null && file.Exists())
            {
                file.Delete();
            }
        }
        catch (Java.IO.IOException)
        {
            // Ignore delete failures
        }
    }

    /// <summary>
    /// Call this method from MainActivity.OnActivityResult
    /// </summary>
    public void OnActivityResult(int requestCode, Result resultCode, Intent? data)
    {
        const string TAG = "FWH_CAMERA";
        Log.Error(TAG, $"OnActivityResult: RequestCode={requestCode}, ExpectedCode={CameraRequestCode}, ResultCode={resultCode}, HasTaskCompletionSource={_photoTcs != null}");
        Logger.OnActivityResultDebug(_logger, requestCode, CameraRequestCode, resultCode, _photoTcs != null);

        if (requestCode != CameraRequestCode || _photoTcs == null)
        {
            return;
        }

        if (resultCode != Result.Ok)
        {
            Logger.CameraResultNonOk(_logger, resultCode);
            CleanupAndComplete(null);
            return;
        }

        try
        {
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
        catch (IOException ex)
        {
            Logger.CameraResultIoError(_logger, ex);
            CleanupAndComplete(null);
        }
        catch (InvalidOperationException ex)
        {
            Logger.CameraResultInvalidState(_logger, ex);
            CleanupAndComplete(null);
        }
    }

    private static partial class Logger
    {
        [LoggerMessage(EventId = 100, Level = LogLevel.Warning, Message = "IsCameraAvailable: Activity not available")]
        public static partial void IsCameraUnavailable(ILogger? logger);

        [LoggerMessage(EventId = 101, Level = LogLevel.Warning, Message = "IsCameraAvailable: PackageManager is null")]
        public static partial void PackageManagerNull(ILogger? logger);

        [LoggerMessage(EventId = 102, Level = LogLevel.Debug, Message = "IsCameraAvailable: HasCamera={HasCamera}, HasCameraAny={HasCameraAny}, PermissionGranted={PermissionGranted}, Result={Result}")]
        public static partial void IsCameraAvailableDebug(ILogger? logger, bool hasCamera, bool hasCameraAny, bool permissionGranted, bool result);

        [LoggerMessage(EventId = 103, Level = LogLevel.Warning, Message = "IsCameraAvailable: Camera hardware available but permission not granted (Status={Status})")]
        public static partial void CameraPermissionMissing(ILogger? logger, Permission status);

        [LoggerMessage(EventId = 104, Level = LogLevel.Warning, Message = "IsCameraAvailable: Activity not available")]
        public static partial void ActivityNotAvailable(ILogger? logger, Exception exception);

        [LoggerMessage(EventId = 110, Level = LogLevel.Error, Message = "TakePhotoAsync: Activity not available")]
        public static partial void ActivityNotAvailableError(ILogger? logger);

        [LoggerMessage(EventId = 111, Level = LogLevel.Error, Message = "Failed to create temp file for camera")]
        public static partial void TempFileCreateFailed(ILogger? logger, Exception exception);

        [LoggerMessage(EventId = 112, Level = LogLevel.Error, Message = "FileProvider.GetUriForFile failed")]
        public static partial void FileProviderFailed(ILogger? logger, Exception exception);

        [LoggerMessage(EventId = 113, Level = LogLevel.Error, Message = "Camera intent could not be resolved - no camera app available or permission denied")]
        public static partial void CameraIntentNotResolved(ILogger? logger);

        [LoggerMessage(EventId = 114, Level = LogLevel.Information, Message = "Starting camera activity: {ActivityName}, RequestCode={RequestCode}")]
        public static partial void StartingCameraActivity(ILogger? logger, string activityName, int requestCode);

        [LoggerMessage(EventId = 115, Level = LogLevel.Debug, Message = "Camera activity started successfully")]
        public static partial void CameraActivityStarted(ILogger? logger);

        [LoggerMessage(EventId = 116, Level = LogLevel.Error, Message = "Failed to start camera activity")]
        public static partial void StartCameraFailed(ILogger? logger, Exception exception);

        [LoggerMessage(EventId = 117, Level = LogLevel.Error, Message = "Exception while starting camera")]
        public static partial void StartCameraInvalidOperation(ILogger? logger, Exception exception);

        [LoggerMessage(EventId = 118, Level = LogLevel.Warning, Message = "Camera not available - IsCameraAvailable returned false")]
        public static partial void CameraNotAvailable(ILogger? logger);

        [LoggerMessage(EventId = 120, Level = LogLevel.Debug, Message = "OnActivityResult called: RequestCode={RequestCode}, ExpectedCode={ExpectedCode}, ResultCode={ResultCode}, HasTaskCompletionSource={HasTaskCompletionSource}")]
        public static partial void OnActivityResultDebug(ILogger? logger, int requestCode, int expectedCode, Result resultCode, bool hasTaskCompletionSource);

        [LoggerMessage(EventId = 121, Level = LogLevel.Warning, Message = "Camera activity returned non-OK result: {ResultCode}")]
        public static partial void CameraResultNonOk(ILogger? logger, Result resultCode);

        [LoggerMessage(EventId = 122, Level = LogLevel.Error, Message = "IO error processing camera result")]
        public static partial void CameraResultIoError(ILogger? logger, Exception exception);

        [LoggerMessage(EventId = 123, Level = LogLevel.Error, Message = "Invalid state processing camera result")]
        public static partial void CameraResultInvalidState(ILogger? logger, Exception exception);
    }
}

/// <summary>
/// Holds the current Activity for Android camera; avoids name conflict with Avalonia.Android.Platform (CA1724).
/// </summary>
public static class AndroidCameraPlatform
{
    public static Activity? CurrentActivity { get; set; }
}
