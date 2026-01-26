using System.Security.Cryptography;
using System.Text;
using FWH.Common.Workflow;
using FWH.Mobile.Data.Repositories;
using Microsoft.Extensions.Logging;

namespace FWH.Mobile.Services;

/// <summary>
/// Service that manages location-based workflow instances.
/// Handles creation, retrieval, and state management of workflows triggered by address changes.
/// </summary>
public class LocationWorkflowService
{
    private readonly IWorkflowService _workflowService;
    private readonly IWorkflowRepository _workflowRepository;
    private readonly ILogger<LocationWorkflowService> _logger;
    private const string LocationWorkflowFileKey = "new-location.puml";
    private const int AddressTimeWindowHours = 24;

    // Cache for workflow file content to avoid repeated file I/O
    private static string? _cachedWorkflowContent;
    private static readonly object _cacheLock = new object();

    public LocationWorkflowService(
        IWorkflowService workflowService,
        IWorkflowRepository workflowRepository,
        ILogger<LocationWorkflowService> logger)
    {
        _workflowService = workflowService ?? throw new ArgumentNullException(nameof(workflowService));
        _workflowRepository = workflowRepository ?? throw new ArgumentNullException(nameof(workflowRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handles a new location address event by starting or resuming appropriate workflow.
    /// </summary>
    /// <param name="eventArgs">Event arguments containing address and location information</param>
    public async Task HandleNewLocationAddressAsync(LocationAddressChangedEventArgs eventArgs)
    {
        ArgumentNullException.ThrowIfNull(eventArgs);
        try
        {
            _logger.LogInformation("Handling new location address: {Address}", eventArgs.CurrentAddress);

            // Generate workflow ID based on address hash
            var addressHash = GenerateAddressHash(eventArgs.CurrentAddress);
            var workflowId = $"location:{addressHash}";

            // Check for existing workflow within 24-hour window
            var since = DateTimeOffset.UtcNow.AddHours(-AddressTimeWindowHours);
            var existingWorkflows = await _workflowRepository.FindByNamePatternAsync(
                workflowId,
                since).ConfigureAwait(false);

            var existingWorkflow = existingWorkflows.FirstOrDefault();

            if (existingWorkflow != null)
            {
                _logger.LogInformation(
                    "Found existing workflow {WorkflowId} for address {Address}, resuming from node {NodeId}",
                    existingWorkflow.Id,
                    eventArgs.CurrentAddress,
                    existingWorkflow.CurrentNodeId);

                // Resume existing workflow
                await _workflowService.StartInstanceAsync(existingWorkflow.Id).ConfigureAwait(false);
            }
            else
            {
                _logger.LogInformation(
                    "No recent workflow found for address {Address}, starting new workflow {WorkflowId}",
                    eventArgs.CurrentAddress,
                    workflowId);

                // Load workflow definition
                var pumlContent = await LoadLocationWorkflowFileAsync().ConfigureAwait(false);
                if (string.IsNullOrEmpty(pumlContent))
                {
                    _logger.LogWarning("Failed to load {FileName}, cannot start location workflow", LocationWorkflowFileKey);
                    return;
                }

                // Import and start new workflow with address-specific variables
                var workflow = await _workflowService.ImportWorkflowAsync(
                    pumlContent,
                    workflowId,
                    $"Location: {eventArgs.CurrentAddress}").ConfigureAwait(false);

                // Set workflow variables
                await SetWorkflowVariablesAsync(workflow.Id, eventArgs).ConfigureAwait(false);

                _logger.LogInformation(
                    "Started new location workflow {WorkflowId} for address {Address}",
                    workflow.Id,
                    eventArgs.CurrentAddress);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling new location address: {Address}", eventArgs.CurrentAddress);
        }
    }

    /// <summary>
    /// Generates a consistent hash for an address to use as workflow key.
    /// </summary>
    private static string GenerateAddressHash(string address)
    {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(address));
        return Convert.ToHexString(hashBytes).Substring(0, 16).ToLowerInvariant();
    }

    /// <summary>
    /// Sets initial variables for a location workflow instance.
    /// </summary>
    private async Task SetWorkflowVariablesAsync(string workflowId, LocationAddressChangedEventArgs eventArgs)
    {
        // Note: Variable setting would require extension to workflow system
        // For now, log the variables that should be set
        _logger.LogDebug(
            "Workflow {WorkflowId} variables: address={Address}, lat={Lat}, lon={Lon}, previous={Previous}, timestamp={Timestamp}",
            workflowId,
            eventArgs.CurrentAddress,
            eventArgs.Location.Latitude,
            eventArgs.Location.Longitude,
            eventArgs.PreviousAddress ?? "none",
            eventArgs.Timestamp);

        // TODO: Once workflow variable system is implemented, set:
        // - address: eventArgs.CurrentAddress
        // - latitude: eventArgs.Location.Latitude
        // - longitude: eventArgs.Location.Longitude
        // - previous_address: eventArgs.PreviousAddress
        // - timestamp: eventArgs.Timestamp
        // - is_first_visit: (eventArgs.PreviousAddress == null)

        await Task.CompletedTask.ConfigureAwait(false);
    }

    /// <summary>
    /// Loads the new-location.puml file from platform-specific location.
    /// Uses caching to avoid repeated file I/O operations.
    /// </summary>
    private async Task<string?> LoadLocationWorkflowFileAsync()
    {
        // Return cached content if available
        lock (_cacheLock)
        {
            if (_cachedWorkflowContent != null)
            {
                _logger.LogDebug("Using cached workflow file content");
                return _cachedWorkflowContent;
            }
        }

        string? content = null;

        // For Android, try loading from assets first
        if (OperatingSystem.IsAndroid())
        {
            try
            {
                var contextType = Type.GetType("Android.App.Application, Mono.Android");
                var contextProperty = contextType?.GetProperty("Context");
                var context = contextProperty?.GetValue(null);

                var assetsProperty = context?.GetType().GetProperty("Assets");
                var assets = assetsProperty?.GetValue(context);

                var openMethod = assets?.GetType().GetMethod("Open", new[] { typeof(string) });
                var stream = openMethod?.Invoke(assets, new object[] { LocationWorkflowFileKey }) as Stream;

                if (stream != null)
                {
                    using (stream)
                    using (var reader = new StreamReader(stream))
                    {
                        content = await reader.ReadToEndAsync().ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load {FileName} from Android assets", LocationWorkflowFileKey);
            }
        }

        // For other platforms, try file system if not loaded from assets
        if (content == null)
        {
            try
            {
                var currentDir = Directory.GetCurrentDirectory();
                var pumlPath = Path.Combine(currentDir, LocationWorkflowFileKey);

                if (!File.Exists(pumlPath))
                {
                    var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                    pumlPath = Path.Combine(baseDir, LocationWorkflowFileKey);
                }

                if (File.Exists(pumlPath))
                {
                    content = await File.ReadAllTextAsync(pumlPath).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load {FileName} from file system", LocationWorkflowFileKey);
            }
        }

        // Cache the content if successfully loaded
        if (content != null)
        {
            lock (_cacheLock)
            {
                _cachedWorkflowContent = content;
                _logger.LogDebug("Cached workflow file content (length: {Length} chars)", content.Length);
            }
        }

        return content;
    }
}
