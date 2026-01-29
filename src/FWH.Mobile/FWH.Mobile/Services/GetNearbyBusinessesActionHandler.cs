using FWH.Common.Location;
using FWH.Common.Location.Models;
using FWH.Common.Workflow.Actions;
using Microsoft.Extensions.Logging;

namespace FWH.Mobile.Services;

/// <summary>
/// Workflow action handler that retrieves current GPS location and finds nearby businesses.
/// Stores results in workflow state for use by subsequent nodes.
/// </summary>
public class GetNearbyBusinessesActionHandler : IWorkflowActionHandler
{
    private readonly IGpsService _gpsService;
    private readonly ILocationService _locationService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<GetNearbyBusinessesActionHandler> _logger;

    public string Name => "get_nearby_businesses";

    public GetNearbyBusinessesActionHandler(
        IGpsService gpsService,
        ILocationService locationService,
        INotificationService notificationService,
        ILogger<GetNearbyBusinessesActionHandler> logger)
    {
        _gpsService = gpsService ?? throw new ArgumentNullException(nameof(gpsService));
        _locationService = locationService ?? throw new ArgumentNullException(nameof(locationService));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IDictionary<string, string>?> HandleAsync(
        ActionHandlerContext context,
        IDictionary<string, string> parameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        _logger.LogInformation("Starting GPS location and nearby business search");

        // Parse radius from parameters (default 1000m)
        var radiusMeters = 1000;
        if (parameters.TryGetValue("radius", out var radiusStr) && int.TryParse(radiusStr, out var radius))
        {
            radiusMeters = radius;
        }

        // Parse categories filter if provided
        IEnumerable<string>? categories = null;
        if (parameters.TryGetValue("categories", out var categoriesStr) && !string.IsNullOrWhiteSpace(categoriesStr))
        {
            categories = categoriesStr.Split(',').Select(c => c.Trim()).Where(c => !string.IsNullOrEmpty(c));
        }

        try
        {
            // Step 1: Check GPS availability
            if (!_gpsService.IsLocationAvailable)
            {
                _logger.LogWarning("GPS location services not available");
                _notificationService.ShowInfo(
                    "Location services are not available. Please enable GPS.",
                    "Location Required");

                // Try to request permission
                var granted = await _gpsService.RequestLocationPermissionAsync().ConfigureAwait(false);
                if (!granted)
                {
                    _logger.LogWarning("GPS permission denied by user");
                    _notificationService.ShowError(
                        "Location permission denied. Cannot find nearby businesses.",
                        "Permission Required");

                    return new Dictionary<string, string>
                    {
                        ["status"] = "permission_denied",
                        ["error"] = "Location permission not granted"
                    };
                }
            }

            // Step 2: Get current GPS location
            _notificationService.ShowInfo("Getting your current location...", "Please Wait");
            _logger.LogInformation("Requesting current GPS coordinates");

            GpsCoordinates? coordinates = null;
            try
            {
                coordinates = await _gpsService.GetCurrentLocationAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (LocationServicesException ex)
            {
                _logger.LogError(ex,
                    "Location service error. Platform: {Platform}, Operation: {Operation}, Diagnostics: {Diagnostics}",
                    ex.Platform,
                    ex.Operation,
                    string.Join(", ", ex.Diagnostics.Select(kvp => $"{kvp.Key}={kvp.Value}")));

                var errorMessage = $"Location service error: {ex.Message}";
                if (ex.Diagnostics.Count > 0)
                {
                    var diagInfo = string.Join(", ", ex.Diagnostics
                        .Where(kvp => kvp.Key != "StackTrace")
                        .Select(kvp => $"{kvp.Key}={kvp.Value}"));
                    errorMessage += $"\nDetails: {diagInfo}";
                }

                _notificationService.ShowError(errorMessage, "Location Service Error");

                return new Dictionary<string, string>
                {
                    ["status"] = "error",
                    ["error"] = ex.Message,
                    ["platform"] = ex.Platform,
                    ["operation"] = ex.Operation,
                    ["diagnostics"] = string.Join("; ", ex.Diagnostics.Select(kvp => $"{kvp.Key}={kvp.Value}"))
                };
            }

            if (coordinates == null || !coordinates.IsValid())
            {
                _logger.LogWarning("Failed to retrieve valid GPS coordinates");
                _notificationService.ShowError(
                    "Could not get your current location. Please check GPS settings.",
                    "Location Error");

                return new Dictionary<string, string>
                {
                    ["status"] = "location_unavailable",
                    ["error"] = "Could not retrieve GPS coordinates"
                };
            }

            _logger.LogInformation("GPS coordinates retrieved: {Latitude}, {Longitude} (accuracy: {Accuracy}m)",
                coordinates.Latitude, coordinates.Longitude, coordinates.AccuracyMeters);

            // Step 3: Find nearby businesses
            _notificationService.ShowInfo($"Finding businesses within {radiusMeters}m...", "Searching");
            _logger.LogInformation("Searching for businesses within {Radius}m at {Latitude}, {Longitude}",
                radiusMeters, coordinates.Latitude, coordinates.Longitude);

            var businesses = await _locationService.GetNearbyBusinessesAsync(
                coordinates.Latitude,
                coordinates.Longitude,
                radiusMeters,
                categories,
                cancellationToken).ConfigureAwait(false);

            var businessList = businesses.ToList();
            _logger.LogInformation("Found {Count} nearby businesses", businessList.Count);

            // Step 4: Prepare results
            var result = new Dictionary<string, string>
            {
                ["status"] = "success",
                ["latitude"] = coordinates.Latitude.ToString("F6"),
                ["longitude"] = coordinates.Longitude.ToString("F6"),
                ["accuracy"] = coordinates.AccuracyMeters?.ToString("F0") ?? "unknown",
                ["radius"] = radiusMeters.ToString(),
                ["count"] = businessList.Count.ToString()
            };

            if (businessList.Any())
            {
                // Store top 5 business names
                var topBusinesses = businessList.Take(5).ToList();
                result["businesses"] = string.Join(",", topBusinesses.Select(b => b.Name));
                result["closest_business"] = topBusinesses.First().Name;
                result["closest_distance"] = (topBusinesses.First().DistanceMeters ?? 0).ToString("F0");

                // Show success notification with details
                var message = $"Found {businessList.Count} businesses nearby!\n\n" +
                              $"Closest: {topBusinesses.First().Name} ({(topBusinesses.First().DistanceMeters ?? 0):F0}m away)";

                if (topBusinesses.Count > 1)
                {
                    message += "\n\nOther nearby:\n" +
                               string.Join("\n", topBusinesses.Skip(1).Take(4).Select(b =>
                                   $"• {b.Name} ({(b.DistanceMeters ?? 0):F0}m)"));
                }

                _notificationService.ShowSuccess(message, "Nearby Businesses");
            }
            else
            {
                // No businesses found
                _notificationService.ShowInfo(
                    $"No businesses found within {radiusMeters}m of your location.",
                    "Search Complete");

                result["businesses"] = string.Empty;
            }

            _logger.LogInformation("Successfully completed location and business search");
            return result;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Location search cancelled by user");
            _notificationService.ShowWarning("Search cancelled", "Cancelled");

            return new Dictionary<string, string>
            {
                ["status"] = "cancelled",
                ["error"] = "Operation cancelled"
            };
        }
        catch (LocationServicesException ex)
        {
            // This should already be handled above, but catch here as a safety net
            _logger.LogError(ex,
                "Location service error in action handler. Platform: {Platform}, Operation: {Operation}",
                ex.Platform,
                ex.Operation);

            _notificationService.ShowError(
                $"Location service error: {ex.Message}",
                "Location Service Error");

            return new Dictionary<string, string>
            {
                ["status"] = "error",
                ["error"] = ex.Message,
                ["platform"] = ex.Platform,
                ["operation"] = ex.Operation
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during location and business search");
            _notificationService.ShowError(
                $"An error occurred: {ex.Message}",
                "Search Error");

            return new Dictionary<string, string>
            {
                ["status"] = "error",
                ["error"] = ex.Message
            };
        }
    }
}
