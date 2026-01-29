using Avalonia.Controls;
using FWH.Mobile.Configuration;
using FWH.Mobile.Data.Data;
using FWH.Mobile.Services;
using FWH.Mobile.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FWH.Mobile.Views;

public partial class PlacesView : UserControl
{
    public PlacesView()
    {
        InitializeComponent();

        Loaded += async (_, _) =>
        {
            var dbContext = App.ServiceProvider.GetRequiredService<NotesDbContext>();
            var logger = App.ServiceProvider.GetRequiredService<ILogger<PlacesViewModel>>();

            // Get device ID from LocationTrackingService (it generates one per instance)
            // We'll use the most recent device ID from the database as a fallback
            var locationTrackingService = App.ServiceProvider.GetRequiredService<ILocationTrackingService>();
            string deviceId;

            // Try to get device ID from existing location records
            try
            {
                var recentLocation = await dbContext.DeviceLocationHistory
                    .OrderByDescending(l => l.Timestamp)
                    .FirstOrDefaultAsync().ConfigureAwait(false);

                deviceId = recentLocation?.DeviceId ?? "default-device";
            }
            catch
            {
                deviceId = "default-device";
            }

            var imageService = App.ServiceProvider.GetService<IImageService>();
            var httpClientFactory = App.ServiceProvider.GetService<System.Net.Http.IHttpClientFactory>();
            var apiSettings = App.ServiceProvider.GetService<ApiSettings>();
            var viewModel = new PlacesViewModel(dbContext, logger, deviceId, imageService, httpClientFactory, apiSettings);
            DataContext = viewModel;

            // Refresh places when view becomes visible
            await viewModel.LoadPlacesAsync().ConfigureAwait(false);
        };
    }
}
