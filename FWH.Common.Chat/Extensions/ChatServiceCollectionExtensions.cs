using System;
using Microsoft.Extensions.DependencyInjection;
using FWH.Common.Chat.ViewModels;
using FWH.Common.Chat.Conversion;
using FWH.Common.Chat.Duplicate;
using FWH.Common.Chat.Services;
using FWH.Common.Imaging.Extensions;

namespace FWH.Common.Chat.Extensions;

/// <summary>
/// Extension methods for registering chat services with dependency injection.
/// Single Responsibility: Configure DI for chat components.
/// </summary>
public static class ChatServiceCollectionExtensions
{
    /// <summary>
    /// Adds all chat services to the service collection.
    /// Includes converter, duplicate detector, chat service, and view models.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddChatServices(this IServiceCollection services)
    {
        // Core chat components (SRP-compliant)
        services.AddSingleton<IWorkflowToChatConverter, WorkflowToChatConverter>();
        services.AddSingleton<IChatDuplicateDetector, ChatDuplicateDetector>();
        services.AddSingleton<ChatService>();
        
        // Platform detection
        services.AddSingleton<IPlatformService, PlatformService>();
        
        // Camera service factory
        services.AddSingleton<CameraServiceFactory>();
        
        // Register camera service using factory
        services.AddSingleton<ICameraService>(sp =>
        {
            var factory = sp.GetRequiredService<CameraServiceFactory>();
            return factory.CreateCameraService();
        });
        
        // View models
        services.AddSingleton<ChatListViewModel>();
        services.AddSingleton<ChatInputViewModel>(sp => 
            new ChatInputViewModel(sp.GetRequiredService<ChatListViewModel>()));
        services.AddSingleton<ChatViewModel>();

        return services;
    }

    /// <summary>
    /// Adds chat service to the service collection.
    /// Requires workflow-to-chat converter and duplicate detector to be registered.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddChatService(this IServiceCollection services)
    {
        services.AddSingleton<ChatService>();
        return services;
    }

    /// <summary>
    /// Adds chat view models to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddChatViewModels(this IServiceCollection services)
    {
        services.AddSingleton<ChatListViewModel>();
        services.AddSingleton<ChatInputViewModel>(sp => 
            new ChatInputViewModel(sp.GetRequiredService<ChatListViewModel>()));
        services.AddSingleton<ChatViewModel>();
        
        return services;
    }

    /// <summary>
    /// Adds workflow-to-chat conversion components to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddWorkflowToChatConversion(this IServiceCollection services)
    {
        services.AddSingleton<IWorkflowToChatConverter, WorkflowToChatConverter>();
        services.AddSingleton<IChatDuplicateDetector, ChatDuplicateDetector>();
        
        return services;
    }

    /// <summary>
    /// Convenience method to register chat-related services plus imaging if desired.
    /// </summary>
    public static IServiceCollection AddChatServicesWithImaging(this IServiceCollection services)
    {
        services.AddChatServices();
        services.AddImagingServices();
        return services;
    }
}
