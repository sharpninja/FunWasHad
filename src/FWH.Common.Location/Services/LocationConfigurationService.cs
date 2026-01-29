using FWH.Common.Location.Configuration;
using FWH.Mobile.Data.Repositories;

namespace FWH.Common.Location.Services;

/// <summary>
/// Service for loading and saving location configuration from database.
/// Single Responsibility: Bridge between LocationServiceOptions and database.
/// </summary>
public class LocationConfigurationService
{
    private readonly IConfigurationRepository _repository;
    private const string CategoryName = "Location";

    // Configuration keys
    private const string KeyDefaultRadius = "Location.DefaultRadiusMeters";
    private const string KeyMaxRadius = "Location.MaxRadiusMeters";
    private const string KeyMinRadius = "Location.MinRadiusMeters";
    private const string KeyTimeout = "Location.TimeoutSeconds";
    private const string KeyUserAgent = "Location.UserAgent";
    private const string KeyApiUrl = "Location.OverpassApiUrl";

    public LocationConfigurationService(IConfigurationRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    /// <summary>
    /// Loads location service options from the database.
    /// If values don't exist, uses defaults and saves them.
    /// </summary>
    public async Task<LocationServiceOptions> LoadOptionsAsync()
    {
        var options = new LocationServiceOptions();

        // Load or initialize default radius (30 meters as requested)
        if (await _repository.ExistsAsync(KeyDefaultRadius).ConfigureAwait(false))
        {
            options.DefaultRadiusMeters = await _repository.GetIntAsync(KeyDefaultRadius, 30).ConfigureAwait(false);
        }
        else
        {
            options.DefaultRadiusMeters = 30;
            await _repository.SetIntAsync(KeyDefaultRadius, 30, CategoryName, "Default search radius in meters").ConfigureAwait(false);
        }

        // Load or initialize max radius
        if (await _repository.ExistsAsync(KeyMaxRadius).ConfigureAwait(false))
        {
            options.MaxRadiusMeters = await _repository.GetIntAsync(KeyMaxRadius, 5000).ConfigureAwait(false);
        }
        else
        {
            options.MaxRadiusMeters = 5000;
            await _repository.SetIntAsync(KeyMaxRadius, 5000, CategoryName, "Maximum allowed search radius in meters").ConfigureAwait(false);
        }

        // Load or initialize min radius
        if (await _repository.ExistsAsync(KeyMinRadius).ConfigureAwait(false))
        {
            options.MinRadiusMeters = await _repository.GetIntAsync(KeyMinRadius, 50).ConfigureAwait(false);
        }
        else
        {
            options.MinRadiusMeters = 50;
            await _repository.SetIntAsync(KeyMinRadius, 50, CategoryName, "Minimum allowed search radius in meters").ConfigureAwait(false);
        }

        // Load or initialize timeout
        if (await _repository.ExistsAsync(KeyTimeout).ConfigureAwait(false))
        {
            options.TimeoutSeconds = await _repository.GetIntAsync(KeyTimeout, 30).ConfigureAwait(false);
        }
        else
        {
            options.TimeoutSeconds = 30;
            await _repository.SetIntAsync(KeyTimeout, 30, CategoryName, "HTTP timeout in seconds").ConfigureAwait(false);
        }

        // Load or initialize user agent
        if (await _repository.ExistsAsync(KeyUserAgent).ConfigureAwait(false))
        {
            options.UserAgent = await _repository.GetStringAsync(KeyUserAgent, "FunWasHad/1.0").ConfigureAwait(false);
        }
        else
        {
            options.UserAgent = "FunWasHad/1.0";
            await _repository.SetAsync(KeyUserAgent, "FunWasHad/1.0", "string", CategoryName, "User agent for HTTP requests").ConfigureAwait(false);
        }

        // Load or initialize API URL
        if (await _repository.ExistsAsync(KeyApiUrl).ConfigureAwait(false))
        {
            options.OverpassApiUrl = await _repository.GetStringAsync(KeyApiUrl, "https://overpass-api.de/api/interpreter").ConfigureAwait(false);
        }
        else
        {
            options.OverpassApiUrl = "https://overpass-api.de/api/interpreter";
            await _repository.SetAsync(KeyApiUrl, "https://overpass-api.de/api/interpreter", "string", CategoryName, "Overpass API endpoint URL").ConfigureAwait(false);
        }

        return options;
    }

    /// <summary>
    /// Saves location service options to the database.
    /// </summary>
    public async Task SaveOptionsAsync(LocationServiceOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        await _repository.SetIntAsync(KeyDefaultRadius, options.DefaultRadiusMeters, CategoryName, "Default search radius in meters").ConfigureAwait(false);
        await _repository.SetIntAsync(KeyMaxRadius, options.MaxRadiusMeters, CategoryName, "Maximum allowed search radius in meters").ConfigureAwait(false);
        await _repository.SetIntAsync(KeyMinRadius, options.MinRadiusMeters, CategoryName, "Minimum allowed search radius in meters").ConfigureAwait(false);
        await _repository.SetIntAsync(KeyTimeout, options.TimeoutSeconds, CategoryName, "HTTP timeout in seconds").ConfigureAwait(false);
        await _repository.SetAsync(KeyUserAgent, options.UserAgent, "string", CategoryName, "User agent for HTTP requests").ConfigureAwait(false);
        await _repository.SetAsync(KeyApiUrl, options.OverpassApiUrl, "string", CategoryName, "Overpass API endpoint URL").ConfigureAwait(false);
    }

    /// <summary>
    /// Updates the default radius in the database.
    /// </summary>
    public async Task SetDefaultRadiusAsync(int radiusMeters)
    {
        await _repository.SetIntAsync(KeyDefaultRadius, radiusMeters, CategoryName, "Default search radius in meters").ConfigureAwait(false);
    }

    /// <summary>
    /// Updates the max radius in the database.
    /// </summary>
    public async Task SetMaxRadiusAsync(int radiusMeters)
    {
        await _repository.SetIntAsync(KeyMaxRadius, radiusMeters, CategoryName, "Maximum allowed search radius in meters").ConfigureAwait(false);
    }

    /// <summary>
    /// Updates the min radius in the database.
    /// </summary>
    public async Task SetMinRadiusAsync(int radiusMeters)
    {
        await _repository.SetIntAsync(KeyMinRadius, radiusMeters, CategoryName, "Minimum allowed search radius in meters").ConfigureAwait(false);
    }
}
