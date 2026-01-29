using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FWH.Mobile.Data.Data;
using FWH.Mobile.Services;
using FWH.Mobile.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FWH.Mobile.ViewModels;

/// <summary>
/// ViewModel for the Settings view.
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly NotesDbContext _dbContext;
    private readonly ILogger<SettingsViewModel> _logger;
    private readonly INotificationService? _notificationService;

    [ObservableProperty]
    private bool _isClearingData;

    public SettingsViewModel(
        NotesDbContext dbContext,
        ILogger<SettingsViewModel> logger,
        INotificationService? notificationService = null)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _notificationService = notificationService;
    }

    [RelayCommand]
    private async Task ClearTrackingDataAsync()
    {
        try
        {
            IsClearingData = true;

            // Get counts before deletion for logging
            var locationCount = await _dbContext.DeviceLocationHistory.CountAsync().ConfigureAwait(false);
            var placesCount = await _dbContext.StationaryPlaces.CountAsync().ConfigureAwait(false);

            _logger.LogInformation("Clearing tracking data: {LocationCount} location records, {PlacesCount} stationary places", locationCount, placesCount);

            // Delete all tracking data
            _dbContext.DeviceLocationHistory.RemoveRange(_dbContext.DeviceLocationHistory);
            _dbContext.StationaryPlaces.RemoveRange(_dbContext.StationaryPlaces);

            var deletedCount = await _dbContext.SaveChangesAsync().ConfigureAwait(false);

            _logger.LogInformation("Successfully cleared {DeletedCount} tracking data records", deletedCount);

            // Show success message
            _notificationService?.ShowSuccess("Tracking data cleared successfully", "Settings");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing tracking data");
            _notificationService?.ShowError($"Failed to clear tracking data: {ex.Message}", "Settings");
        }
        finally
        {
            IsClearingData = false;
        }
    }

    [RelayCommand]
    private async Task ShowClearDataConfirmationAsync()
    {
        var dialog = new ConfirmationDialog
        {
            Title = "Clear Tracking Data",
            Message = "Are you sure you want to clear all tracking data? This will delete all location history and stationary places. This action cannot be undone.",
            ConfirmText = "Clear Data",
            CancelText = "Cancel"
        };

        var result = await dialog.ShowDialogAsync().ConfigureAwait(false);
        
        if (result == true)
        {
            await ClearTrackingDataAsync().ConfigureAwait(false);
        }
    }
}
