using Avalonia.Controls;
using FWH.Mobile.Data.Data;
using FWH.Mobile.Services;
using FWH.Mobile.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FWH.Mobile.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();

        Loaded += (_, _) =>
        {
            // Resolve ViewModel from DI container
            var dbContext = App.ServiceProvider.GetRequiredService<NotesDbContext>();
            var logger = App.ServiceProvider.GetRequiredService<ILogger<SettingsViewModel>>();
            var notificationService = App.ServiceProvider.GetService(typeof(INotificationService)) as INotificationService;
            DataContext = new SettingsViewModel(dbContext, logger, notificationService);
        };
    }
}
