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
using Microsoft.Extensions.DependencyInjection;

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

namespace FWH.Mobile;

public partial class App : Application
{
    private static bool _isDatabaseInitialized = false;
    private static readonly SemaphoreSlim _initializationLock = new SemaphoreSlim(1, 1);

    static App()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        // Register data services (includes configuration repository)
        services.AddDataServices();

        // Register workflow services using extension method
        services.AddWorkflowServices();

        // Register chat services using extension method
        // This now includes platform detection and camera service factory
        services.AddChatServices();

        // Register location services (includes GPS service factory)
        // Requires IPlatformService from AddChatServices() to be registered first
        services.AddLocationServices();

        // Register typed client that talks to the Location Web API
        var apiBaseAddress = Environment.GetEnvironmentVariable("LOCATION_API_BASE_URL") ?? "https://localhost:5001/";
        services.Configure<LocationApiClientOptions>(options =>
        {
            options.BaseAddress = apiBaseAddress;
            options.Timeout = TimeSpan.FromSeconds(30);
        });
        services.AddHttpClient<ILocationService, LocationApiClient>();

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

        ServiceProvider = services.BuildServiceProvider();

        // Database initialization deferred to OnFrameworkInitializationCompleted
        // to avoid blocking the UI thread during app startup
    }

    public static IServiceProvider ServiceProvider { get; }

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
    /// Ensures the database is initialized. Safe to call multiple times.
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
            var dbContext = scope.ServiceProvider.GetRequiredService<FWH.Mobile.Data.Data.NotesDbContext>();

            // Create database if it doesn't exist
            await dbContext.Database.EnsureCreatedAsync();

            _isDatabaseInitialized = true;
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

    public override async void OnFrameworkInitializationCompleted()
    {
        // Initialize database asynchronously before setting up the UI
        await EnsureDatabaseInitializedAsync();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
            DisableAvaloniaDataAnnotationValidation();

            // Initialize workflow from workflow.puml
            await InitializeWorkflowAsync();

            var chatViewModel = ServiceProvider.GetRequiredService<ChatViewModel>();

            var mainWindow = new MainWindow
            {
                DataContext = chatViewModel,
                Width = 800,
                Height = 600,
                Title = "Fun Was Had"
            };

            desktop.MainWindow = mainWindow;
            mainWindow.Show();
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            // Initialize workflow from workflow.puml
            await InitializeWorkflowAsync();

            var chatViewModel = ServiceProvider.GetRequiredService<ChatViewModel>();

            singleViewPlatform.MainView = new MainView
            {
                DataContext = chatViewModel
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private async Task InitializeWorkflowAsync()
    {
        // Always start chat to ensure initial messages are shown even if workflow file is missing
        var chatService = ServiceProvider.GetRequiredService<ChatService>();
        await chatService.StartAsync();

        string? pumlPath = null;
        string? pumlContent = null;

        try
        {
            // Load workflow.puml file
            var currentDir = Directory.GetCurrentDirectory();
            pumlPath = Path.Combine(currentDir, "workflow.puml");

            if (!File.Exists(pumlPath))
            {
                // Try alternative path (for development/deployment scenarios)
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var altPath = Path.Combine(baseDir, "workflow.puml");

                if (File.Exists(altPath))
                {
                    pumlPath = altPath;
                }
            }

            if (pumlPath != null && File.Exists(pumlPath))
            {
                pumlContent = await File.ReadAllTextAsync(pumlPath);
            }
            else
            {
                // Fallback workflow so Android still shows camera node even without file
                pumlContent = "@startuml\nstart\n:camera;\nnote right: Take a photo of where you are\nif (Was fun had?) then (yes)\n  :Record Fun Experience;\nelse (no)\n  :Record Not Fun Experience;\nendif\nstop\n@enduml";
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
