using FWH.Mobile.Data.Entities;

namespace FWH.Mobile.Data.Repositories;

/// <summary>
/// Repository for managing configuration settings in the database.
/// Single Responsibility: CRUD operations for configuration settings.
/// </summary>
public interface IConfigurationRepository
{
    /// <summary>
    /// Gets a configuration value by key.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <returns>The configuration setting or null if not found.</returns>
    Task<ConfigurationSetting?> GetByKeyAsync(string key);

    /// <summary>
    /// Gets all configuration settings in a category.
    /// </summary>
    /// <param name="category">The category name.</param>
    /// <returns>Collection of configuration settings.</returns>
    Task<IEnumerable<ConfigurationSetting>> GetByCategoryAsync(string category);

    /// <summary>
    /// Gets all configuration settings.
    /// </summary>
    /// <returns>Collection of all configuration settings.</returns>
    Task<IEnumerable<ConfigurationSetting>> GetAllAsync();

    /// <summary>
    /// Sets or updates a configuration value.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <param name="value">The configuration value.</param>
    /// <param name="valueType">The data type of the value.</param>
    /// <param name="category">Optional category.</param>
    /// <param name="description">Optional description.</param>
    Task SetAsync(string key, string value, string valueType = "string", string? category = null, string? description = null);

    /// <summary>
    /// Sets an integer configuration value.
    /// </summary>
    Task SetIntAsync(string key, int value, string? category = null, string? description = null);

    /// <summary>
    /// Sets a boolean configuration value.
    /// </summary>
    Task SetBoolAsync(string key, bool value, string? category = null, string? description = null);

    /// <summary>
    /// Gets an integer configuration value.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <param name="defaultValue">Default value if key not found.</param>
    /// <returns>The configuration value or default.</returns>
    Task<int> GetIntAsync(string key, int defaultValue = 0);

    /// <summary>
    /// Gets a boolean configuration value.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <param name="defaultValue">Default value if key not found.</param>
    /// <returns>The configuration value or default.</returns>
    Task<bool> GetBoolAsync(string key, bool defaultValue = false);

    /// <summary>
    /// Gets a string configuration value.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <param name="defaultValue">Default value if key not found.</param>
    /// <returns>The configuration value or default.</returns>
    Task<string> GetStringAsync(string key, string defaultValue = "");

    /// <summary>
    /// Deletes a configuration setting.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    Task DeleteAsync(string key);

    /// <summary>
    /// Checks if a configuration key exists.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <returns>True if the key exists.</returns>
    Task<bool> ExistsAsync(string key);
}
