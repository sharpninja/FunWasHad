using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using FWH.Mobile.Configuration;
using Microsoft.Extensions.Logging;

namespace FWH.Mobile.Services;

/// <summary>
/// Service for managing and applying themes to the Avalonia application.
/// Retrieves themes from the Marketing API and applies them to application resources.
/// </summary>
public class ThemeService : IThemeService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ApiSettings _apiSettings;
    private readonly ILogger<ThemeService> _logger;
    private string? _currentThemeName;

    // Theme resource keys
    private const string PrimaryColorKey = "ThemePrimaryColor";
    private const string SecondaryColorKey = "ThemeSecondaryColor";
    private const string AccentColorKey = "ThemeAccentColor";
    private const string BackgroundColorKey = "ThemeBackgroundColor";
    private const string TextColorKey = "ThemeTextColor";

    public ThemeService(
        IHttpClientFactory httpClientFactory,
        ApiSettings apiSettings,
        ILogger<ThemeService> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _apiSettings = apiSettings ?? throw new ArgumentNullException(nameof(apiSettings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string? CurrentThemeName => _currentThemeName;

    /// <summary>
    /// Applies a business theme to the application.
    /// </summary>
    public async Task<bool> ApplyBusinessThemeAsync(long businessId)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient("MarketingApi");
            var themeUrl = $"api/marketing/{businessId}/theme";

            var theme = await httpClient.GetFromJsonAsync<BusinessThemeDto>(themeUrl);

            if (theme == null || !theme.IsActive)
            {
                _logger.LogDebug("No active theme found for business {BusinessId}", businessId);
                return false;
            }

            ApplyTheme(theme.ThemeName, theme.PrimaryColor, theme.SecondaryColor,
                theme.AccentColor, theme.BackgroundColor, theme.TextColor);

            _logger.LogInformation("Applied business theme '{ThemeName}' for business {BusinessId}",
                theme.ThemeName, businessId);

            return true;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve business theme for business {BusinessId}", businessId);
            return false;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "Request timeout retrieving business theme for business {BusinessId}", businessId);
            return false;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Applying business theme was cancelled for business {BusinessId}", businessId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying business theme for business {BusinessId}", businessId);
            return false;
        }
    }

    /// <summary>
    /// Applies a city theme to the application.
    /// </summary>
    public async Task<bool> ApplyCityThemeAsync(string cityName, string? state = null, string? country = null)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient("MarketingApi");
            var queryBuilder = new System.Text.StringBuilder();
            queryBuilder.Append("api/marketing/city?cityName=");
            queryBuilder.Append(Uri.EscapeDataString(cityName));
            if (!string.IsNullOrEmpty(state))
            {
                queryBuilder.Append("&state=");
                queryBuilder.Append(Uri.EscapeDataString(state));
            }
            if (!string.IsNullOrEmpty(country))
            {
                queryBuilder.Append("&country=");
                queryBuilder.Append(Uri.EscapeDataString(country));
            }

            var cityUrl = queryBuilder.ToString();
            var cityResponse = await httpClient.GetFromJsonAsync<CityMarketingResponseDto>(cityUrl);

            if (cityResponse?.Theme == null || !cityResponse.Theme.IsActive)
            {
                _logger.LogDebug("No active theme found for city {CityName}", cityName);
                return false;
            }

            var theme = cityResponse.Theme;
            ApplyTheme(theme.ThemeName, theme.PrimaryColor, theme.SecondaryColor,
                theme.AccentColor, theme.BackgroundColor, theme.TextColor);

            _logger.LogInformation("Applied city theme '{ThemeName}' for city {CityName}",
                theme.ThemeName, cityName);

            return true;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve city theme for city {CityName}", cityName);
            return false;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "Request timeout retrieving city theme for city {CityName}", cityName);
            return false;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Applying city theme was cancelled for city {CityName}", cityName);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying city theme for city {CityName}", cityName);
            return false;
        }
    }

    /// <summary>
    /// Resets the theme to default (removes any applied theme).
    /// </summary>
    public void ResetToDefaultTheme()
    {
        var app = Application.Current;
        if (app == null)
            return;

        var resources = app.Resources;

        // Remove theme colors
        resources.Remove(PrimaryColorKey);
        resources.Remove(SecondaryColorKey);
        resources.Remove(AccentColorKey);
        resources.Remove(BackgroundColorKey);
        resources.Remove(TextColorKey);

        _currentThemeName = null;
        _logger.LogInformation("Reset to default theme");
    }

    /// <summary>
    /// Applies theme colors to application resources.
    /// </summary>
    private void ApplyTheme(string themeName, string? primaryColor, string? secondaryColor,
        string? accentColor, string? backgroundColor, string? textColor)
    {
        var app = Application.Current;
        if (app == null)
        {
            _logger.LogWarning("Cannot apply theme: Application.Current is null");
            return;
        }

        var resources = app.Resources;

        // Apply colors if provided
        if (!string.IsNullOrEmpty(primaryColor) && TryParseColor(primaryColor, out var primary))
        {
            resources[PrimaryColorKey] = primary;
        }

        if (!string.IsNullOrEmpty(secondaryColor) && TryParseColor(secondaryColor, out var secondary))
        {
            resources[SecondaryColorKey] = secondary;
        }

        if (!string.IsNullOrEmpty(accentColor) && TryParseColor(accentColor, out var accent))
        {
            resources[AccentColorKey] = accent;
        }

        if (!string.IsNullOrEmpty(backgroundColor) && TryParseColor(backgroundColor, out var background))
        {
            resources[BackgroundColorKey] = background;
        }

        if (!string.IsNullOrEmpty(textColor) && TryParseColor(textColor, out var text))
        {
            resources[TextColorKey] = text;
        }

        _currentThemeName = themeName;
    }

    /// <summary>
    /// Attempts to parse a color string (hex format like #RRGGBB or #AARRGGBB) into an Avalonia Color.
    /// </summary>
    private static bool TryParseColor(string colorString, out Color color)
    {
        color = Colors.Transparent;

        if (string.IsNullOrWhiteSpace(colorString))
            return false;

        // Remove # if present
        var hex = colorString.TrimStart('#');

        // Handle both 6-digit and 8-digit hex colors
        if (hex.Length == 6)
        {
            // Add alpha channel (fully opaque)
            hex = "FF" + hex;
        }

        if (hex.Length != 8)
            return false;

        try
        {
            var argb = Convert.ToUInt32(hex, 16);
            color = Color.FromUInt32(argb);
            return true;
        }
        catch
        {
            return false;
        }
    }

    // DTOs for API responses
    private class BusinessThemeDto
    {
        public string ThemeName { get; set; } = string.Empty;
        public string? PrimaryColor { get; set; }
        public string? SecondaryColor { get; set; }
        public string? AccentColor { get; set; }
        public string? BackgroundColor { get; set; }
        public string? TextColor { get; set; }
        public bool IsActive { get; set; }
    }

    private class CityMarketingResponseDto
    {
        public CityThemeDto? Theme { get; set; }
    }

    private class CityThemeDto
    {
        public string ThemeName { get; set; } = string.Empty;
        public string? PrimaryColor { get; set; }
        public string? SecondaryColor { get; set; }
        public string? AccentColor { get; set; }
        public string? BackgroundColor { get; set; }
        public string? TextColor { get; set; }
        public bool IsActive { get; set; }
    }
}
