using Orchestrix.Contracts.Mediator;

namespace Orchestrix.Contracts.Location;

/// <summary>
/// Base request for location-related operations.
/// </summary>
public abstract record LocationRequest : IMediatorRequest<LocationResponse>;

/// <summary>
/// Base response for location-related operations.
/// </summary>
public record LocationResponse
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Request to update device location.
/// </summary>
public record UpdateDeviceLocationRequest : IMediatorRequest<UpdateDeviceLocationResponse>
{
    public required string DeviceId { get; init; }
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public double? Accuracy { get; init; }
    public double? Altitude { get; init; }
    public double? Speed { get; init; }
    public double? Heading { get; init; }
    public DateTimeOffset Timestamp { get; init; }
}

/// <summary>
/// Response to update device location.
/// </summary>
public record UpdateDeviceLocationResponse : LocationResponse
{
    public long? LocationId { get; init; }
}

/// <summary>
/// Request to get device location history.
/// </summary>
public record GetDeviceLocationHistoryRequest : IMediatorRequest<GetDeviceLocationHistoryResponse>
{
    public required string DeviceId { get; init; }
    public DateTimeOffset? Since { get; init; }
    public DateTimeOffset? Until { get; init; }
    public int? Limit { get; init; }
}

/// <summary>
/// Response to get device location history.
/// </summary>
public record GetDeviceLocationHistoryResponse : LocationResponse
{
    public List<DeviceLocationDto> Locations { get; init; } = new();
}

/// <summary>
/// Request to get nearby businesses.
/// </summary>
public record GetNearbyBusinessesRequest : IMediatorRequest<GetNearbyBusinessesResponse>
{
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public int RadiusMeters { get; init; } = 100;
    public string[]? Tags { get; init; }
}

/// <summary>
/// Response to get nearby businesses.
/// </summary>
public record GetNearbyBusinessesResponse : LocationResponse
{
    public List<BusinessDto> Businesses { get; init; } = new();
    public int TotalCount { get; init; }
}

/// <summary>
/// DTO for device location.
/// </summary>
public record DeviceLocationDto
{
    public long Id { get; init; }
    public required string DeviceId { get; init; }
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public double? Accuracy { get; init; }
    public double? Altitude { get; init; }
    public double? Speed { get; init; }
    public double? Heading { get; init; }
    public DateTimeOffset Timestamp { get; init; }
}

/// <summary>
/// DTO for business information.
/// </summary>
public record BusinessDto
{
    public long Id { get; init; }
    public required string Name { get; init; }
    public required string Address { get; init; }
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public double? DistanceMeters { get; init; }
    public string? Category { get; init; }
    public string? PhoneNumber { get; init; }
    public string? Website { get; init; }
}
