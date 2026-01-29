using System.Runtime.InteropServices;

namespace FWH.Common.Chat.Services;

/// <summary>
/// Default platform detection service using runtime information
/// </summary>
public class PlatformService : IPlatformService
{
    private readonly PlatformType _platform;

    public PlatformService()
    {
        _platform = DetectPlatform();
    }

    public PlatformType Platform => _platform;

    public bool IsAndroid => _platform == PlatformType.Android;

    public bool IsIOS => _platform == PlatformType.iOS;

    public bool IsDesktop => _platform == PlatformType.Desktop;

    public bool IsBrowser => _platform == PlatformType.Browser;

    public string GetDatabasePath(string databaseName)
    {
        string basePath;

        if (IsAndroid)
        {
            // On Android, use the app's private storage directory
            // This is accessible via Android.App.Application.Context.GetExternalFilesDir(null)
            // Use reflection to avoid compile-time dependency on Android assemblies
            try
            {
                var contextType = Type.GetType("Android.App.Application, Mono.Android");
                var contextProperty = contextType?.GetProperty("Context");
                var context = contextProperty?.GetValue(null);

                var getExternalFilesDirMethod = context?.GetType().GetMethod("GetExternalFilesDir", new[] { typeof(string) });
                var filesDir = getExternalFilesDirMethod?.Invoke(context, new object?[] { null });

                var absolutePathProperty = filesDir?.GetType().GetProperty("AbsolutePath");
                basePath = absolutePathProperty?.GetValue(filesDir) as string ?? "/data/data/com.CompanyName.FWH.Mobile/files";
            }
            catch
            {
                // Fallback to a default Android path
                basePath = "/data/data/com.CompanyName.FWH.Mobile/files";
            }
        }
        else if (IsIOS)
        {
            // On iOS, use the Documents directory
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            basePath = Path.Combine(documentsPath, "..", "Library");
        }
        else if (IsBrowser)
        {
            // For browser/WASM, use in-memory or IndexedDB (not implemented here)
            return "DataSource=:memory:";
        }
        else
        {
            // Desktop: use application data directory
            basePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            basePath = Path.Combine(basePath, "FWH.Mobile");
        }

        // Ensure the directory exists
        if (!Directory.Exists(basePath))
        {
            Directory.CreateDirectory(basePath);
        }

        return Path.Combine(basePath, databaseName);
    }

    private static PlatformType DetectPlatform()
    {
        // Check for browser/WASM first
        if (RuntimeInformation.OSDescription.Contains("Browser", StringComparison.OrdinalIgnoreCase) ||
            OperatingSystem.IsBrowser())
        {
            return PlatformType.Browser;
        }

        // Check for Android
        if (OperatingSystem.IsAndroid())
        {
            return PlatformType.Android;
        }

        // Check for iOS/tvOS/watchOS/macCatalyst
        if (OperatingSystem.IsIOS() ||
            OperatingSystem.IsTvOS() ||
            OperatingSystem.IsWatchOS() ||
            OperatingSystem.IsMacCatalyst())
        {
            return PlatformType.iOS;
        }

        // Check for desktop platforms
        if (OperatingSystem.IsWindows() ||
            OperatingSystem.IsLinux() ||
            OperatingSystem.IsMacOS())
        {
            return PlatformType.Desktop;
        }

        return PlatformType.Unknown;
    }
}
