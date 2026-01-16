using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;

using Avalonia;
using Avalonia.Android;

using FWH.Mobile.Droid.Services;
using FWH.Common.Chat.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FWH.Mobile.Android;
[Activity(
    Label = "FWH.Mobile.Android",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity<App>
{
    private AndroidCameraService? _cameraService;
    private const int PermissionsRequestCode = 100;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        
        // Set current activity for camera service
        Platform.CurrentActivity = this;
        
        // Get camera service instance (if registered in DI)
        _cameraService = App.ServiceProvider.GetService<ICameraService>() as AndroidCameraService;
        
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
        base.OnActivityResult(requestCode, resultCode, data);
        
        // Forward result to camera service
        _cameraService?.OnActivityResult(requestCode, resultCode, data);
    }

    private void RequestRequiredPermissions()
    {
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
    }

    public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
    {
        base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        
        if (requestCode == PermissionsRequestCode)
        {
            for (int i = 0; i < permissions.Length; i++)
            {
                var permission = permissions[i];
                var result = grantResults[i];
                
                if (result == Permission.Granted)
                {
                    System.Diagnostics.Debug.WriteLine($"Permission granted: {permission}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Permission denied: {permission}");
                }
            }
        }
    }
}
