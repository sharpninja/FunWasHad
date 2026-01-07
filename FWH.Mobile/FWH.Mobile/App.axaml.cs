using System;
using System.Linq;
using System.IO;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;

using FWH.Mobile.ViewModels;
using FWH.Mobile.Views;
using FWH.Common.Chat.ViewModels;
using FWH.Common.Workflow;
using FWH.Common.Workflow.Extensions;
using FWH.Common.Chat;
using FWH.Common.Chat.Extensions;
using FWH.Common.Location.Extensions;
using FWH.Mobile.Data.Extensions;

namespace FWH.Mobile;
public partial class App : Application
{
    static App()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        // Register data services (includes configuration repository)
        services.AddDataServices();

        // Register workflow services using extension method
        services.AddWorkflowServices();

        // Register chat services using extension method
        services.AddChatServices();

        // Register location services (will load configuration from database)
        // Default radius: 30 meters (persisted to SQLite)
        services.AddLocationServices();

        ServiceProvider = services.BuildServiceProvider();

        // Ensure database is created
        InitializeDatabaseAsync().GetAwaiter().GetResult();
    }

    internal static IServiceProvider ServiceProvider { get; }

    private static async System.Threading.Tasks.Task InitializeDatabaseAsync()
    {
        using var scope = ServiceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FWH.Mobile.Data.Data.NotesDbContext>();
        
        // Create database if it doesn't exist
        await dbContext.Database.EnsureCreatedAsync();
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override async void OnFrameworkInitializationCompleted()
    {
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

    private async System.Threading.Tasks.Task InitializeWorkflowAsync()
    {
        try
        {
            // Load workflow.puml file
            var currentDir = Directory.GetCurrentDirectory();
            var pumlPath = Path.Combine(currentDir, "workflow.puml");
            
            if (!File.Exists(pumlPath))
            {
                // Try alternative path (for development/deployment scenarios)
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var altPath = Path.Combine(baseDir, "workflow.puml");
                
                if (File.Exists(altPath))
                {
                    pumlPath = altPath;
                }
                else
                {
                    return;
                }
            }

            var pumlContent = await File.ReadAllTextAsync(pumlPath);

            // Import the workflow
            var workflowService = ServiceProvider.GetRequiredService<IWorkflowService>();
            var workflow = await workflowService.ImportWorkflowAsync(
                pumlContent, 
                "fun-was-had-main", 
                "Fun Was Had");

            // Get the chat service and render the initial workflow state
            var chatService = ServiceProvider.GetRequiredService<ChatService>();
            var chatViewModel = ServiceProvider.GetRequiredService<ChatViewModel>();
            var chatList = ServiceProvider.GetRequiredService<ChatListViewModel>();

            // Start the chat service (this sets up initial welcome message)
            await chatService.StartAsync();

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
