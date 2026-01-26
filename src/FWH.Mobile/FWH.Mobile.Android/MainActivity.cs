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

namespace FWH.Mobile.Android;

[Activity(
    Label = "#FunWasHad",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity<App>
{
    private AndroidCameraService? _cameraService;
    private ILogger<MainActivity>? _logger;
    private const int PermissionsRequestCode = 100;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        savedInstanceState ??= new(0);

        // Prevents issues with Avalonia restoring state
        savedInstanceState.Remove("Avalonia");
        base.OnCreate(savedInstanceState);

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
            _logger?.LogInformation("Camera service instance retrieved successfully");
        }
        else
        {
            _logger?.LogWarning("Camera service instance not found in DI container");
        }

        // Request all required permissions on startup
        RequestRequiredPermissions();

    }

    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        return base.CustomizeAppBuilder(builder)
            .WithInterFont();
    }

    protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
    {
        const string TAG = "FWH_CAMERA";
        base.OnActivityResult(requestCode, resultCode, data);

        Log.Error(TAG, $"MainActivity.OnActivityResult: RequestCode={requestCode}, ResultCode={resultCode}, HasData={data != null}");
        _logger?.LogDebug("OnActivityResult: RequestCode={RequestCode}, ResultCode={ResultCode}, HasData={HasData}",
            requestCode, resultCode, data != null);

        // Forward result to camera service
        if (_cameraService != null)
        {
            Log.Error(TAG, "OnActivityResult: Forwarding to camera service");
            _cameraService.OnActivityResult(requestCode, resultCode, data);
        }
        else
        {
            Log.Error(TAG, "OnActivityResult: Camera service is null, trying to retrieve again");
            _logger?.LogWarning("OnActivityResult: Camera service is null, cannot forward result");
            // Try to get camera service again in case it wasn't available during OnCreate
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

#pragma warning disable CA1416 // Platform compatibility - protected by version check in RequestRequiredPermissions
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
                    _logger?.LogInformation("Permission granted: {Permission}", permission);
                }
                else
                {
                    _logger?.LogWarning("Permission denied: {Permission}", permission);
                }
            }
        }
#pragma warning restore CA1416
    }
}
