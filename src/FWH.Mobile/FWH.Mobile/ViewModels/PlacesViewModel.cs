using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using FWH.Mobile.Configuration;
using FWH.Mobile.Data.Data;
using FWH.Mobile.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FWH.Mobile.ViewModels;

/// <summary>
/// ViewModel for displaying places where the user became stationary.
/// Shows a reverse chronological list of businesses and locations.
/// </summary>
public class PlacesViewModel : INotifyPropertyChanged
{
    private readonly NotesDbContext _dbContext;
    private readonly ILogger<PlacesViewModel> _logger;
    private readonly string _deviceId;
    private readonly IImageService? _imageService;
    private readonly IHttpClientFactory? _httpClientFactory;
    private readonly ApiSettings? _apiSettings;
    private bool _isLoading;
    private readonly ObservableCollection<PlaceItem> _allPlaces = new();

    public PlacesViewModel(
        NotesDbContext dbContext,
        ILogger<PlacesViewModel> logger,
        string deviceId,
        IImageService? imageService = null,
        IHttpClientFactory? httpClientFactory = null,
        ApiSettings? apiSettings = null)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _deviceId = deviceId ?? throw new ArgumentNullException(nameof(deviceId));
        _imageService = imageService;
        _httpClientFactory = httpClientFactory;
        _apiSettings = apiSettings;
        Places = new ObservableCollection<PlaceItem>();
        Places.CollectionChanged += (_, _) => OnPropertyChanged(nameof(IsEmpty));

        DeletePlaceCommand = new RelayCommand<PlaceItem>(DeletePlace);
        ToggleFavoriteCommand = new RelayCommand<PlaceItem>(ToggleFavorite);
        FilterModes = new[] { "All", "Favorites" };

        _ = LoadPlacesAsync();
    }

    public ObservableCollection<PlaceItem> Places { get; }

    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (_isLoading != value)
            {
                _isLoading = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsEmpty));
            }
        }
    }

    public bool IsEmpty => !IsLoading && Places.Count == 0;

    public ICommand DeletePlaceCommand { get; }
    public ICommand ToggleFavoriteCommand { get; }

    public string[] FilterModes { get; }

    private string _filterMode = "All";
    public string FilterMode
    {
        get => _filterMode;
        set
        {
            if (_filterMode != value)
            {
                _filterMode = value;
                OnPropertyChanged();
                ApplyFilter();
            }
        }
    }

    public async Task LoadPlacesAsync()
    {
        try
        {
            IsLoading = true;

            var places = await _dbContext.StationaryPlaces
                .Where(p => p.DeviceId == _deviceId)
                .OrderByDescending(p => p.StationaryAt)
                .ToListAsync().ConfigureAwait(false);

            _allPlaces.Clear();
            var placesNeedingLogoFetch = new List<(PlaceItem item, string businessName, double lat, double lon)>();

            foreach (var place in places)
            {
                var city = ExtractCityFromAddress(place.Address);
                var localTime = ConvertToLocalTime(place.StationaryAt, place.Longitude);

                var placeItem = new PlaceItem
                {
                    Id = place.Id,
                    BusinessName = place.BusinessName,
                    Address = place.Address,
                    City = city,
                    Category = place.Category,
                    Latitude = place.Latitude,
                    Longitude = place.Longitude,
                    StationaryAt = place.StationaryAt,
                    LocalTime = localTime,
                    IsFavorite = place.IsFavorite,
                    // Use cached marketing info if available
                    LogoUrl = place.LogoUrl,
                    PrimaryColor = place.PrimaryColor,
                    SecondaryColor = place.SecondaryColor,
                    AccentColor = place.AccentColor,
                    BackgroundColor = place.BackgroundColor,
                    TextColor = place.TextColor,
                    BackgroundImageUrl = place.BackgroundImageUrl
                };

                _allPlaces.Add(placeItem);

                // Only fetch logo if business name is available and marketing info is not cached or stale
                if (!string.IsNullOrEmpty(place.BusinessName) &&
                    (place.MarketingInfoCachedAt == null ||
                     place.MarketingInfoCachedAt < DateTimeOffset.UtcNow.AddHours(-24)))
                {
                    placesNeedingLogoFetch.Add((placeItem, place.BusinessName, place.Latitude, place.Longitude));
                }
            }

            ApplyFilter();
            _logger.LogInformation("Loaded {Count} places", _allPlaces.Count);

            // Batch fetch logos for places that need them (limit to avoid overwhelming the API)
            if (placesNeedingLogoFetch.Count > 0)
            {
                _logger.LogDebug("Fetching marketing info for {Count} places", placesNeedingLogoFetch.Count);
                // Process in batches to avoid overwhelming the API
                const int batchSize = 5;
                for (int i = 0; i < placesNeedingLogoFetch.Count; i += batchSize)
                {
                    var batch = placesNeedingLogoFetch.Skip(i).Take(batchSize);
                    var tasks = batch.Select(p => FetchBusinessLogoAsync(p.item, p.businessName, p.lat, p.lon));
                    await Task.WhenAll(tasks).ConfigureAwait(false);
                }
            }
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error loading places");
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Loading places was cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading places");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ApplyFilter()
    {
        Places.Clear();
        var filtered = _filterMode == "Favorites"
            ? _allPlaces.Where(p => p.IsFavorite)
            : _allPlaces;

        foreach (var place in filtered.OrderByDescending(p => p.StationaryAt))
        {
            Places.Add(place);
        }
    }

    private async void ToggleFavorite(PlaceItem? place)
    {
        if (place == null)
            return;

        try
        {
            var entity = await _dbContext.StationaryPlaces.FindAsync(place.Id).ConfigureAwait(false);
            if (entity != null)
            {
                entity.IsFavorite = !entity.IsFavorite;
                await _dbContext.SaveChangesAsync().ConfigureAwait(false);

                place.IsFavorite = entity.IsFavorite;
                _logger.LogInformation("Toggled favorite for place: {PlaceId}, IsFavorite: {IsFavorite}", place.Id, place.IsFavorite);

                // Reapply filter if we're showing favorites
                if (_filterMode == "Favorites" && !place.IsFavorite)
                {
                    ApplyFilter();
                }
            }
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error toggling favorite for place: {PlaceId}", place.Id);
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Toggle favorite operation was cancelled for place: {PlaceId}", place.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling favorite for place: {PlaceId}", place.Id);
        }
    }

    private async void DeletePlace(PlaceItem? place)
    {
        if (place == null)
            return;

        try
        {
            var entity = await _dbContext.StationaryPlaces.FindAsync(place.Id).ConfigureAwait(false);
            if (entity != null)
            {
                _dbContext.StationaryPlaces.Remove(entity);
                await _dbContext.SaveChangesAsync().ConfigureAwait(false);

                _allPlaces.Remove(place);
                Places.Remove(place);
                _logger.LogInformation("Deleted place: {PlaceId}", place.Id);
            }
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error deleting place: {PlaceId}", place.Id);
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Delete place operation was cancelled for place: {PlaceId}", place.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting place: {PlaceId}", place.Id);
        }
    }

    private static string? ExtractCityFromAddress(string? address)
    {
        if (string.IsNullOrEmpty(address))
            return null;

        // Try to extract city from address
        // Address format is typically: "Street, City, State ZIP" or "Street, City, Country"
        var parts = address.Split(',');
        if (parts.Length >= 2)
        {
            // City is usually the second-to-last or last part
            // Try to find a part that looks like a city (not a ZIP code)
            for (int i = parts.Length - 1; i >= 0; i--)
            {
                var part = parts[i].Trim();
                // Skip if it looks like a ZIP code (all digits or digits with dash)
                if (System.Text.RegularExpressions.Regex.IsMatch(part, @"^\d{5}(-\d{4})?$"))
                    continue;
                // Skip if it's very short (likely state abbreviation)
                if (part.Length <= 3 && part.All(char.IsLetter))
                    continue;
                // Return the first part that looks like a city
                if (part.Length > 3)
                    return part;
            }
        }

        return null;
    }

    private async Task FetchBusinessLogoAsync(PlaceItem placeItem, string businessName, double latitude, double longitude)
    {
        if (_httpClientFactory == null || _apiSettings == null)
        {
            _logger.LogDebug("HttpClientFactory or ApiSettings not available, skipping logo fetch for {BusinessName}", businessName);
            return;
        }

        if (string.IsNullOrWhiteSpace(businessName))
        {
            _logger.LogDebug("Business name is empty, skipping logo fetch");
            return;
        }

        try
        {
            // Create HTTP client for marketing API
            using var httpClient = _httpClientFactory.CreateClient("MarketingApi");
            var marketingApiUri = _apiSettings.GetResolvedMarketingApiBaseUrl();
            httpClient.BaseAddress = marketingApiUri;
            httpClient.Timeout = TimeSpan.FromSeconds(10);

            // Find nearby businesses in marketing API
            var nearbyUrl = $"api/marketing/nearby?latitude={latitude.ToString(CultureInfo.InvariantCulture)}&longitude={longitude.ToString(CultureInfo.InvariantCulture)}&radiusMeters=100";
            var businesses = await httpClient.GetFromJsonAsync<List<MarketingBusinessDto>>(nearbyUrl).ConfigureAwait(false);

            if (businesses == null || !businesses.Any())
            {
                _logger.LogDebug("No nearby businesses found for {BusinessName} at ({Lat}, {Lon})", businessName, latitude, longitude);
                return;
            }

            // Try to match by business name (case-insensitive, partial match)
            var matchedBusiness = businesses.FirstOrDefault(b =>
                !string.IsNullOrEmpty(b.Name) &&
                b.Name.Equals(businessName, StringComparison.OrdinalIgnoreCase));

            // If no exact match, try partial match
            if (matchedBusiness == null)
            {
                matchedBusiness = businesses.FirstOrDefault(b =>
                    !string.IsNullOrEmpty(b.Name) &&
                    (b.Name.Contains(businessName, StringComparison.OrdinalIgnoreCase) ||
                     businessName.Contains(b.Name, StringComparison.OrdinalIgnoreCase)));
            }

            if (matchedBusiness == null)
            {
                _logger.LogDebug("No matching business found for {BusinessName}", businessName);
                return;
            }

            // Get business theme to retrieve marketing info
            var themeUrl = $"api/marketing/{matchedBusiness.Id}/theme";
            var theme = await httpClient.GetFromJsonAsync<BusinessThemeDto>(themeUrl).ConfigureAwait(false);

            if (theme == null)
            {
                _logger.LogDebug("No theme found for business {BusinessId}", matchedBusiness.Id);
                return;
            }

            // Store images via ImageService if available
            if (_imageService != null)
            {
                if (!string.IsNullOrEmpty(theme.LogoUrl))
                {
                    await _imageService.GetImageAsync(
                        theme.LogoUrl,
                        "business_logo",
                        "Business",
                        matchedBusiness.Id).ConfigureAwait(false);
                }

                if (!string.IsNullOrEmpty(theme.BackgroundImageUrl))
                {
                    await _imageService.GetImageAsync(
                        theme.BackgroundImageUrl,
                        "business_background",
                        "Business",
                        matchedBusiness.Id).ConfigureAwait(false);
                }
            }

            // Update PlaceItem for UI
            placeItem.LogoUrl = theme.LogoUrl;
            placeItem.PrimaryColor = theme.PrimaryColor;
            placeItem.SecondaryColor = theme.SecondaryColor;
            placeItem.AccentColor = theme.AccentColor;
            placeItem.BackgroundColor = theme.BackgroundColor;
            placeItem.TextColor = theme.TextColor;
            placeItem.BackgroundImageUrl = theme.BackgroundImageUrl;

            // Update database entity
            var entity = await _dbContext.StationaryPlaces.FindAsync(placeItem.Id).ConfigureAwait(false);
            if (entity != null)
            {
                entity.BusinessId = matchedBusiness.Id;
                entity.LogoUrl = theme.LogoUrl;
                entity.PrimaryColor = theme.PrimaryColor;
                entity.SecondaryColor = theme.SecondaryColor;
                entity.AccentColor = theme.AccentColor;
                entity.BackgroundColor = theme.BackgroundColor;
                entity.TextColor = theme.TextColor;
                entity.BackgroundImageUrl = theme.BackgroundImageUrl;
                entity.MarketingInfoCachedAt = DateTimeOffset.UtcNow;
                await _dbContext.SaveChangesAsync().ConfigureAwait(false);
            }

            _logger.LogDebug("Refreshed marketing info for business: {BusinessName}, BusinessId: {BusinessId}",
                businessName, matchedBusiness.Id);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "HTTP error refreshing marketing info for business: {BusinessName}", businessName);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "Request timeout refreshing marketing info for business: {BusinessName}", businessName);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogWarning(ex, "Database error saving marketing info for business: {BusinessName}", businessName);
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Marketing info fetch cancelled for business: {BusinessName}", businessName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error refreshing marketing info for business: {BusinessName}", businessName);
        }
    }

    private static DateTimeOffset ConvertToLocalTime(DateTimeOffset utcTime, double longitude)
    {
        // Simple timezone approximation: each 15 degrees of longitude ≈ 1 hour
        // This is a rough approximation and doesn't account for political boundaries
        // For a production app, you'd want to use a proper timezone service
        var offsetHours = (int)Math.Round(longitude / 15.0);
        var timeZoneOffset = TimeSpan.FromHours(offsetHours);

        return new DateTimeOffset(utcTime.DateTime, timeZoneOffset);
    }

    // DTOs for Marketing API responses
    private class MarketingBusinessDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    private class BusinessThemeDto
    {
        public long Id { get; set; }
        public long BusinessId { get; set; }
        public string ThemeName { get; set; } = string.Empty;
        public string? LogoUrl { get; set; }
        public string? PrimaryColor { get; set; }
        public string? SecondaryColor { get; set; }
        public string? AccentColor { get; set; }
        public string? BackgroundColor { get; set; }
        public string? TextColor { get; set; }
        public string? BackgroundImageUrl { get; set; }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private sealed class RelayCommand<T> : ICommand
    {
        private readonly Action<T?> _execute;

        public RelayCommand(Action<T?> execute) => _execute = execute ?? throw new ArgumentNullException(nameof(execute));

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => _execute(parameter is T item ? item : default);

        public event EventHandler? CanExecuteChanged { add { } remove { } }
    }
}

/// <summary>
/// Represents a place item for display in the UI.
/// </summary>
public class PlaceItem : INotifyPropertyChanged
{
    private string? _logoUrl;

    public long Id { get; set; }
    public string? BusinessName { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Category { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateTimeOffset StationaryAt { get; set; }
    public DateTimeOffset LocalTime { get; set; }
    public bool IsFavorite { get; set; }

    public string? LogoUrl
    {
        get => _logoUrl;
        set
        {
            if (_logoUrl != value)
            {
                _logoUrl = value;
                OnPropertyChanged();
            }
        }
    }

    public string? PrimaryColor { get; set; }
    public string? SecondaryColor { get; set; }
    public string? AccentColor { get; set; }
    public string? BackgroundColor { get; set; }
    public string? TextColor { get; set; }
    public string? BackgroundImageUrl { get; set; }

    public string DisplayName => BusinessName ?? Address ?? $"{Latitude:F6}, {Longitude:F6}";
    public string DisplayAddress => Address ?? $"{Latitude:F6}, {Longitude:F6}";

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
