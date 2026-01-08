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
    private const int CameraPermissionRequestCode = 100;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        
        // Set current activity for camera service
        Platform.CurrentActivity = this;
        
        // Get camera service instance (if registered in DI)
        _cameraService = App.ServiceProvider.GetService<ICameraService>() as AndroidCameraService;
        
        // Request camera permission on startup
        RequestCameraPermissionIfNeeded();
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

    private void RequestCameraPermissionIfNeeded()
    {
        if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
        {
            if (CheckSelfPermission(global::Android.Manifest.Permission.Camera) != Permission.Granted)
            {
                RequestPermissions(new[] { global::Android.Manifest.Permission.Camera }, CameraPermissionRequestCode);
            }
        }
    }

    public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
    {
        base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        
        if (requestCode == CameraPermissionRequestCode)
        {
            if (grantResults.Length > 0 && grantResults[0] == Permission.Granted)
            {
                // Permission granted - camera can now be used
                System.Diagnostics.Debug.WriteLine("Camera permission granted");
            }
            else
            {
                // Permission denied - show message to user
                System.Diagnostics.Debug.WriteLine("Camera permission denied");
            }
        }
    }
}
