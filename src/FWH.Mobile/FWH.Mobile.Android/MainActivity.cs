using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Util;
using Avalonia;
using Avalonia.Android;
using FWH.Common.Chat.Services;
using FWH.Mobile.Droid.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Projektanker.Icons.Avalonia;
using Projektanker.Icons.Avalonia.FontAwesome;
using System.Runtime.Versioning;

namespace FWH.Mobile.Android;

[Activity(
    Label = "#FunWasHad",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public partial class MainActivity : AvaloniaMainActivity<App>
{
    private AndroidCameraService? _cameraService;
    private ILogger<MainActivity>? _logger;
    private const int PermissionsRequestCode = 100;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        Bundle? createdBundle = null;
        if (savedInstanceState is null)
        {
            createdBundle = new Bundle();
            savedInstanceState = createdBundle;
        }

        // Prevents issues with Avalonia restoring state
        savedInstanceState.Remove("Avalonia");
        base.OnCreate(savedInstanceState);

        createdBundle?.Dispose();

        // Set current activity for camera service
        AndroidCameraPlatform.CurrentActivity = this;

        // Get logger from service provider
        _logger = App.ServiceProvider?.GetService<ILogger<MainActivity>>();

        // Get camera service instance (if registered in DI)
        // Try both keyed and non-keyed service resolution
        _cameraService = App.ServiceProvider?.GetService<ICameraService>() as AndroidCameraService;
        if (_cameraService == null)
        {
            _cameraService = App.ServiceProvider?.GetKeyedService<ICameraService>("Android") as AndroidCameraService;
        }

        if (_cameraService != null)
        {
            LogMessages.CameraServiceRetrieved(_logger);
        }
        else
        {
            LogMessages.CameraServiceMissing(_logger);
        }

        // Request all required permissions on startup
        RequestRequiredPermissions();

    }

    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        // Register Font Awesome icon provider.
        IconProvider.Current
            .Register<FontAwesomeIconProvider>();
        Log.Info("FWH", "Icon provider = Font Awesome.");

        return base.CustomizeAppBuilder(builder)
            .WithInterFont();
    }

    protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
    {
        const string TAG = "FWH_CAMERA";
        base.OnActivityResult(requestCode, resultCode, data);

        Log.Error(TAG, $"MainActivity.OnActivityResult: RequestCode={requestCode}, ResultCode={resultCode}, HasData={data != null}");
        LogMessages.OnActivityResult(_logger, requestCode, resultCode, data != null);

        if (_cameraService != null)
        {
            Log.Error(TAG, "OnActivityResult: Forwarding to camera service");
            _cameraService.OnActivityResult(requestCode, resultCode, data);
        }
        else
        {
            Log.Error(TAG, "OnActivityResult: Camera service is null, trying to retrieve again");
            LogMessages.CameraServiceNull(_logger);
            _cameraService = App.ServiceProvider?.GetService<ICameraService>() as AndroidCameraService;
            if (_cameraService != null)
            {
                Log.Error(TAG, "OnActivityResult: Camera service retrieved, forwarding result");
                _cameraService.OnActivityResult(requestCode, resultCode, data);
            }
            else
            {
                Log.Error(TAG, "OnActivityResult: Camera service still null, cannot forward result");
            }
        }
    }

    private void RequestRequiredPermissions()
    {
#pragma warning disable CA1416 // Platform compatibility - protected by version check
        if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
        {
            var permissionsToRequest = new System.Collections.Generic.List<string>();

            // Check camera permission
            if (CheckSelfPermission(global::Android.Manifest.Permission.Camera) != Permission.Granted)
            {
                permissionsToRequest.Add(global::Android.Manifest.Permission.Camera);
            }

            // Check location permissions
            if (CheckSelfPermission(global::Android.Manifest.Permission.AccessFineLocation) != Permission.Granted)
            {
                permissionsToRequest.Add(global::Android.Manifest.Permission.AccessFineLocation);
            }

            if (CheckSelfPermission(global::Android.Manifest.Permission.AccessCoarseLocation) != Permission.Granted)
            {
                permissionsToRequest.Add(global::Android.Manifest.Permission.AccessCoarseLocation);
            }

            // Request all missing permissions at once
            if (permissionsToRequest.Count > 0)
            {
                RequestPermissions(permissionsToRequest.ToArray(), PermissionsRequestCode);
            }
        }
#pragma warning restore CA1416
    }

    [SupportedOSPlatform("android23.0")]
    public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
    {
        ArgumentNullException.ThrowIfNull(permissions);
        ArgumentNullException.ThrowIfNull(grantResults);
        base.OnRequestPermissionsResult(requestCode, permissions, grantResults);

        if (requestCode == PermissionsRequestCode)
        {
            for (int i = 0; i < permissions.Length; i++)
            {
                var permission = permissions[i];
                var result = grantResults[i];

                if (result == Permission.Granted)
                {
                    LogMessages.PermissionGranted(_logger, permission);
                }
                else
                {
                    LogMessages.PermissionDenied(_logger, permission);
                }
            }
        }
    }

    private static partial class LogMessages
    {
        [LoggerMessage(EventId = 200, Level = LogLevel.Information, Message = "Camera service instance retrieved successfully")]
        public static partial void CameraServiceRetrieved(ILogger? logger);

        [LoggerMessage(EventId = 201, Level = LogLevel.Warning, Message = "Camera service instance not found in DI container")]
        public static partial void CameraServiceMissing(ILogger? logger);

        [LoggerMessage(EventId = 202, Level = LogLevel.Debug, Message = "OnActivityResult: RequestCode={RequestCode}, ResultCode={ResultCode}, HasData={HasData}")]
        public static partial void OnActivityResult(ILogger? logger, int requestCode, Result resultCode, bool hasData);

        [LoggerMessage(EventId = 203, Level = LogLevel.Warning, Message = "OnActivityResult: Camera service is null, cannot forward result")]
        public static partial void CameraServiceNull(ILogger? logger);

        [LoggerMessage(EventId = 204, Level = LogLevel.Information, Message = "Permission granted: {Permission}")]
        public static partial void PermissionGranted(ILogger? logger, string permission);

        [LoggerMessage(EventId = 205, Level = LogLevel.Warning, Message = "Permission denied: {Permission}")]
        public static partial void PermissionDenied(ILogger? logger, string permission);
    }
}
