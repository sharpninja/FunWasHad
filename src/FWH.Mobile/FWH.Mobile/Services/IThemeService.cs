using System.Threading.Tasks;

namespace FWH.Mobile.Services;

/// <summary>
/// Service for managing and applying themes to the application.
/// Supports both business themes (when at a business location) and city themes (when in a city).
/// </summary>
public interface IThemeService
{
    /// <summary>
    /// Applies a business theme to the application.
    /// </summary>
    /// <param name="businessId">The business ID</param>
    /// <returns>True if theme was applied successfully, false otherwise</returns>
    Task<bool> ApplyBusinessThemeAsync(long businessId);

    /// <summary>
    /// Applies a city theme to the application.
    /// </summary>
    /// <param name="cityName">City name</param>
    /// <param name="state">State or province (optional)</param>
    /// <param name="country">Country (optional)</param>
    /// <returns>True if theme was applied successfully, false otherwise</returns>
    Task<bool> ApplyCityThemeAsync(string cityName, string? state = null, string? country = null);

    /// <summary>
    /// Resets the theme to default (removes any applied theme).
    /// </summary>
    void ResetToDefaultTheme();

    /// <summary>
    /// Gets the currently applied theme name, or null if default theme is active.
    /// </summary>
    string? CurrentThemeName { get; }
}
