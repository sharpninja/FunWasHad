using System;
using System.Linq;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using FWH.Mobile.Configuration;
using FWH.Mobile.ViewModels;
using FWH.Mobile.Views;
using FWH.Common.Chat.Services;
using FWH.Common.Chat.ViewModels;
using FWH.Common.Workflow;
using FWH.Common.Workflow.Extensions;
using FWH.Common.Chat;
using FWH.Common.Chat.Extensions;
using FWH.Common.Location;
using FWH.Common.Location.Extensions;
using FWH.Mobile.Data.Extensions;
using FWH.Common.Imaging.Extensions;
using FWH.Mobile.Options;
using FWH.Mobile.Services;
using FWH.Orchestrix.Mediator.Remote.Extensions;

namespace FWH.Mobile;

public partial class App : Application
{
    private static bool _isDatabaseInitialized = false;
    private static readonly SemaphoreSlim _initializationLock = new SemaphoreSlim(1, 1);

    static App()
    {
        // Build configuration
        var configuration = BuildConfiguration();

        var services = new ServiceCollection();

        // Register configuration
        services.AddSingleton<IConfiguration>(configuration);

        // Register API settings
        var apiSettings = configuration.GetSection("ApiSettings").Get<ApiSettings>() ?? new ApiSettings();
        services.AddSingleton(apiSettings);

        // Register log store and configure logging to use Avalonia logger
        var logStore = new FWH.Mobile.Logging.AvaloniaLogStore(maxEntries: 1000);
        services.AddSingleton(logStore);

        services.AddLogging(builder =>
        {
            builder.AddConfiguration(configuration.GetSection("Logging"));
            builder.AddConsole(); // Enable console logging for Android logcat
            builder.AddProvider(new FWH.Mobile.Logging.AvaloniaLoggerProvider(logStore));
        });

        // Note: Service discovery is available when Microsoft.Extensions.ServiceDiscovery package is added
        // For now, using direct URL configuration for mobile app
        // services.AddServiceDiscovery();

        // Register platform service first (needed for database path)
        services.AddSingleton<IPlatformService, PlatformService>();

        // Get the platform-specific database path
        var platformService = new PlatformService();
        var databasePath = platformService.GetDatabasePath("fwh_mobile.db");
        var connectionString = $"DataSource={databasePath}";

        // Register data services with persistent database
        services.AddDataServices(connectionString);

        // Register workflow services using extension method
        services.AddWorkflowServices();

        // Register chat services using extension method
        // This now includes platform detection and camera service factory
        services.AddChatServices();

        // Register location services (includes GPS service factory)
        // Requires IPlatformService from AddChatServices() to be registered first
        services.AddLocationServices();


        // Register MediatR handlers for remote API calls
        services.AddRemoteMediatorHandlers();

        // Get API base addresses from configuration
        string locationApiBaseAddress;
        string marketingApiBaseAddress;

        // Check for environment variable overrides first
        var locationEnvVar = Environment.GetEnvironmentVariable("LOCATION_API_BASE_URL");
        var marketingEnvVar = Environment.GetEnvironmentVariable("MARKETING_API_BASE_URL");

        if (!string.IsNullOrEmpty(locationEnvVar) && !string.IsNullOrEmpty(marketingEnvVar))
        {
            // Use environment variables if set
            locationApiBaseAddress = locationEnvVar;
            marketingApiBaseAddress = marketingEnvVar;
        }
        else
        {
            // Use configured URLs from appsettings (supports both full URLs for staging and IP/port for development)
            locationApiBaseAddress = apiSettings.GetLocationApiBaseUrl();
            marketingApiBaseAddress = apiSettings.GetMarketingApiBaseUrl();
        }

        services.AddApiHttpClients(options =>
        {
            options.LocationApiBaseUrl = locationApiBaseAddress;
            options.MarketingApiBaseUrl = marketingApiBaseAddress;
        });

        // Keep ILocationService registered for GetNearbyBusinessesActionHandler and other services that need it
        services.AddSingleton<ILocationService>(sp =>
        {
            var clientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            var httpClient = clientFactory.CreateClient("LocationApi");

            return new LocationApiClient(
                httpClient,
                Microsoft.Extensions.Options.Options.Create(new LocationApiClientOptions
                {
                    BaseAddress = locationApiBaseAddress,
                    Timeout = TimeSpan.FromSeconds(30)
                }),
                loggerFactory.CreateLogger<LocationApiClient>());
        });

        // Register location tracking service
        services.AddSingleton<ILocationTrackingService, LocationTrackingService>();

        // Register location workflow service for address-based workflows
        services.AddSingleton<LocationWorkflowService>();

        // Register activity tracking service
        services.AddSingleton<ActivityTrackingService>();

        // Register activity tracking ViewModel
        services.AddSingleton<ActivityTrackingViewModel>();

        // Register movement state logger (for demonstration)
        services.AddSingleton<MovementStateLogger>();

        // Register movement state ViewModel
        services.AddSingleton<MovementStateViewModel>();

        // Register imaging services
        services.AddImagingServices();

        // Register notification service (uses chat UI for notifications)
        services.AddSingleton<INotificationService, ChatNotificationService>();

        // Register workflow action handler for GPS + nearby businesses
        services.AddScoped<GetNearbyBusinessesActionHandler>();
        services.AddScoped<FWH.Common.Workflow.Actions.IWorkflowActionHandler>(sp =>
            sp.GetRequiredService<GetNearbyBusinessesActionHandler>());

        // Register platform-specific camera services using runtime detection
        // These extension methods will be no-ops if the platform assembly isn't loaded
        TryRegisterPlatformCameraServices(services);

        // Register camera capture ViewModel
        services.AddSingleton<CameraCaptureViewModel>();

        // Register log viewer ViewModel
        services.AddSingleton<LogViewerViewModel>();

        ServiceProvider = services.BuildServiceProvider();
        ServiceProvider.InitializeWorkflowActionHandlers();

        // Database initialization deferred to OnFrameworkInitializationCompleted
        // to avoid blocking the UI thread during app startup

    }

    public static IServiceProvider ServiceProvider { get; }

    /// <summary>
    /// Builds the application configuration from appsettings.json files
    /// </summary>
    private static IConfiguration BuildConfiguration()
    {
        var builder = new ConfigurationBuilder();

        // Get environment name (Staging, Development, Production, etc.)
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? "Development";

        if (OperatingSystem.IsAndroid())
        {
            // On Android, read from assets
            var appSettingsStream = LoadAndroidAsset("appsettings.json");
            if (appSettingsStream != null)
            {
                builder.AddJsonStream(appSettingsStream);
            }

            // Load environment-specific appsettings file
            var envSettingsStream = LoadAndroidAsset($"appsettings.{environment}.json");
            if (envSettingsStream != null)
            {
                builder.AddJsonStream(envSettingsStream);
            }
        }
        else
        {
            // On Desktop/iOS, read from file system
            builder
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true);
        }

        builder.AddEnvironmentVariables();

        return builder.Build();
    }

    /// <summary>
    /// Loads a file from Android assets.
    /// </summary>
    /// <param name="fileName">The name of the asset file</param>
    /// <returns>Stream containing the asset content, or null if not found</returns>
    private static Stream? LoadAndroidAsset(string fileName)
    {
        if (!OperatingSystem.IsAndroid())
            return null;

        try
        {
            // Use reflection to access Android assets to avoid compile-time dependency
            var contextType = Type.GetType("Android.App.Application, Mono.Android");
            var contextProperty = contextType?.GetProperty("Context");
            var context = contextProperty?.GetValue(null);

            var assetsProperty = context?.GetType().GetProperty("Assets");
            var assets = assetsProperty?.GetValue(context);

            var openMethod = assets?.GetType().GetMethod("Open", new[] { typeof(string) });
            var stream = openMethod?.Invoke(assets, new object[] { fileName }) as Stream;

            if (stream != null)
            {
                System.Diagnostics.Debug.WriteLine($"Loaded Android asset: {fileName}");
                return stream;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load Android asset '{fileName}': {ex.Message}");
        }

        return null;
    }

    /// <summary>
    /// Gets the host machine's IP address from configuration.
    /// The IP address is populated at build time by the MSBuild target in FWH.Mobile.csproj.
    /// </summary>
    /// <param name="configuration">The configuration instance</param>
    /// <returns>The host IP address from configuration, or null if not set</returns>
    private static string? GetHostIpAddress(IConfiguration configuration)
    {
        var hostIp = configuration["ApiSettings:HostIpAddress"];

        if (string.IsNullOrEmpty(hostIp) || hostIp == "HOST_IP_PLACEHOLDER")
        {
            return null;
        }

        return hostIp;
    }

    /// <summary>
    /// Attempts to register platform-specific camera and GPS services using reflection
    /// to avoid compile-time dependencies
    /// </summary>
    private static void TryRegisterPlatformCameraServices(IServiceCollection services)
    {
        try
        {
            // Try to find and invoke Android extension methods
            var androidAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "FWH.Mobile.Android");

            if (androidAssembly != null)
            {
                var androidExtensions = androidAssembly.GetType("FWH.Mobile.Android.AndroidServiceCollectionExtensions");

                // Register camera service
                var addAndroidCameraMethod = androidExtensions?.GetMethod("AddAndroidCameraService");
                addAndroidCameraMethod?.Invoke(null, new object[] { services });

                // Register GPS service
                var addAndroidGpsMethod = androidExtensions?.GetMethod("AddAndroidGpsService");
                addAndroidGpsMethod?.Invoke(null, new object[] { services });
            }
        }
        catch
        {
            // Silently ignore - Android platform not available
        }

        try
        {
            // Try to find and invoke iOS extension methods
            var iosAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "FWH.Mobile.iOS");

            if (iosAssembly != null)
            {
                var iosExtensions = iosAssembly.GetType("FWH.Mobile.iOS.iOSServiceCollectionExtensions");

                // Register camera service
                var addIOSCameraMethod = iosExtensions?.GetMethod("AddIOSCameraService");
                addIOSCameraMethod?.Invoke(null, new object[] { services });

                // Register GPS service
                var addIOSGpsMethod = iosExtensions?.GetMethod("AddIOSGpsService");
                addIOSGpsMethod?.Invoke(null, new object[] { services });
            }
        }
        catch
        {
            // Silently ignore - iOS platform not available
        }

        try
        {
            // Try to find and invoke Desktop extension methods
            var desktopAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "FWH.Mobile.Desktop");

            if (desktopAssembly != null)
            {
                var desktopExtensions = desktopAssembly.GetType("FWH.Mobile.Desktop.DesktopServiceCollectionExtensions");

                // Register GPS service (Desktop doesn't have camera service yet)
                var addDesktopGpsMethod = desktopExtensions?.GetMethod("AddDesktopGpsService");
                addDesktopGpsMethod?.Invoke(null, new object[] { services });
            }
        }
        catch
        {
            // Silently ignore - Desktop platform not available or Windows SDK not available
        }
    }

    /// <summary>
    /// Ensures the database is initialized with all migrations applied.
    /// Safe to call multiple times.
    /// TR-MOBILE-001: Includes DeviceLocationHistory table for local tracking.
    /// </summary>
    private static async Task EnsureDatabaseInitializedAsync()
    {
        if (_isDatabaseInitialized)
            return;

        await _initializationLock.WaitAsync();
        try
        {
            if (_isDatabaseInitialized)
                return;

            using var scope = ServiceProvider.CreateScope();
            var migrationService = scope.ServiceProvider.GetRequiredService<FWH.Mobile.Data.Services.MobileDatabaseMigrationService>();
            var logger = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<App>>();

            // Log database connection info for debugging
            var connectionInfo = migrationService.GetConnectionInfo();
            logger.LogDebug("Initializing database at: {ConnectionInfo}", connectionInfo);

            // Ensure database exists and apply any pending migrations
            await migrationService.EnsureDatabaseAsync();

            logger.LogInformation("Database initialization completed successfully");

            _isDatabaseInitialized = true;
        }
        catch (Exception ex)
        {
            using var scope = ServiceProvider.CreateScope();
            var logger = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<App>>();
            logger.LogError(ex, "Failed to initialize database: {Message}", ex.Message);
            throw;
        }
        finally
        {
            _initializationLock.Release();
        }
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Create UI immediately to prevent ANR - don't await anything before showing UI
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit.
            DisableAvaloniaDataAnnotationValidation();

            var chatViewModel = ServiceProvider.GetRequiredService<ChatViewModel>();
            var logViewerViewModel = ServiceProvider.GetRequiredService<LogViewerViewModel>();

            var mainWindow = new MainWindow
            {
                DataContext = chatViewModel,
                Tag = logViewerViewModel, // Pass log viewer ViewModel via Tag
                Width = 800,
                Height = 600,
                Title = "Fun Was Had"
            };

            desktop.MainWindow = mainWindow;
            mainWindow.Show();

            // Initialize everything in background to avoid blocking UI
            _ = Task.Run(async () =>
            {
                try
                {
                    // Initialize database with timeout to prevent hanging
                    using var dbCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                    await EnsureDatabaseInitializedAsync().WaitAsync(dbCts.Token).ConfigureAwait(false);

                    // Initialize workflow with timeout
                    using var workflowCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                    await InitializeWorkflowAsync().WaitAsync(workflowCts.Token).ConfigureAwait(false);

                    // Start location tracking (already has internal timeouts)
                    await StartLocationTrackingAsync().ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    System.Diagnostics.Debug.WriteLine("Background initialization timed out");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Background initialization error: {ex.Message}");
                }
            });
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            // Create MainView immediately on UI thread - don't await anything
            var chatViewModel = ServiceProvider.GetRequiredService<ChatViewModel>();

            singleViewPlatform.MainView = new MainView
            {
                DataContext = chatViewModel
            };

            // Initialize everything in background to avoid ANR
            // Use Task.Run to ensure it's on a background thread
            _ = Task.Run(async () =>
            {
                try
                {
                    // Initialize database first with timeout to prevent hanging
                    using var dbCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                    await EnsureDatabaseInitializedAsync().WaitAsync(dbCts.Token).ConfigureAwait(false);

                    // Then initialize workflow with timeout
                    using var workflowCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                    await InitializeWorkflowAsync().WaitAsync(workflowCts.Token).ConfigureAwait(false);

                    // Finally start location tracking (already has internal timeouts)
                    await StartLocationTrackingAsync().ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    System.Diagnostics.Debug.WriteLine("Background initialization timed out - app will continue");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Background initialization error: {ex.Message}");
                }
            });
        }

        base.OnFrameworkInitializationCompleted();
    }

    private async Task StartLocationTrackingAsync()
    {
        try
        {
            var trackingService = ServiceProvider.GetRequiredService<ILocationTrackingService>();

            // Start tracking with default settings (50m threshold, 30s interval)
            await trackingService.StartTrackingAsync();

            System.Diagnostics.Debug.WriteLine("Location tracking started successfully");

            // Start activity tracking service
            var activityTracking = ServiceProvider.GetRequiredService<ActivityTrackingService>();
            activityTracking.StartMonitoring();

            System.Diagnostics.Debug.WriteLine("Activity tracking started successfully");

            // Start movement state logger for demonstration
            var stateLogger = ServiceProvider.GetRequiredService<MovementStateLogger>();
            stateLogger.StartLogging();

            System.Diagnostics.Debug.WriteLine("Movement state logging started successfully");
        }
        catch (Exception ex)
        {
            // Don't fail app startup if location tracking fails
            System.Diagnostics.Debug.WriteLine($"Failed to start location tracking: {ex.Message}");
        }
    }

    private async Task InitializeWorkflowAsync()
    {
        // Always start chat to ensure initial messages are shown even if workflow file is missing
        var chatService = ServiceProvider.GetRequiredService<ChatService>();
        await chatService.StartAsync();

        string? pumlContent = null;

        try
        {
            // Try to load workflow.puml from platform-specific location
            pumlContent = await LoadWorkflowFileAsync();

            if (string.IsNullOrEmpty(pumlContent))
            {
                // Fallback workflow if file cannot be loaded
                throw new InvalidOperationException("Cannot locate workflow definition file.");
            }

            // Import the workflow
            var workflowService = ServiceProvider.GetRequiredService<IWorkflowService>();
            var workflow = await workflowService.ImportWorkflowAsync(
                pumlContent,
                "fun-was-had-main",
                "Fun Was Had");

            // Render the first activity node from the workflow
            await chatService.RenderWorkflowStateAsync(workflow.Id);
        }
        catch (Exception)
        {
            // Silently fail - workflow initialization is optional
        }
    }

    /// <summary>
    /// Loads workflow.puml file from platform-specific location
    /// </summary>
    private async Task<string?> LoadWorkflowFileAsync()
    {
        // For Android, try loading from assets first
        if (OperatingSystem.IsAndroid())
        {
            try
            {
                // On Android, assets are accessed via reflection to avoid compile-time dependency
                var contextType = Type.GetType("Android.App.Application, Mono.Android");
                var contextProperty = contextType?.GetProperty("Context");
                var context = contextProperty?.GetValue(null);

                var assetsProperty = context?.GetType().GetProperty("Assets");
                var assets = assetsProperty?.GetValue(context);

                var openMethod = assets?.GetType().GetMethod("Open", new[] { typeof(string) });
                var stream = openMethod?.Invoke(assets, new object[] { "workflow.puml" }) as Stream;

                if (stream != null)
                {
                    using (stream)
                    using (var reader = new StreamReader(stream))
                    {
                        return await reader.ReadToEndAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load workflow.puml from Android assets: {ex.Message}");
            }
        }

        // For other platforms (Desktop, iOS, Browser), try file system
        try
        {
            var currentDir = Directory.GetCurrentDirectory();
            var pumlPath = Path.Combine(currentDir, "workflow.puml");

            if (!File.Exists(pumlPath))
            {
                // Try alternative path (for development/deployment scenarios)
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                pumlPath = Path.Combine(baseDir, "workflow.puml");
            }

            if (File.Exists(pumlPath))
            {
                return await File.ReadAllTextAsync(pumlPath);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load workflow.puml from file system: {ex.Message}");
        }

        return null;
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}
