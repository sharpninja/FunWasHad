namespace FWH.Mobile.Services;

/// <summary>
/// Utility class for GPS coordinate calculations.
/// </summary>
public static class GpsCalculator
{
    private const double EarthRadiusMeters = 6371000.0; // Earth's radius in meters
    private const double MpsToMphFactor = 2.23694; // Conversion factor
    private const double MpsToKmhFactor = 3.6; // Conversion factor

    /// <summary>
    /// Calculates the distance between two GPS coordinates using the Haversine formula.
    /// </summary>
    /// <param name="lat1">Latitude of first point in degrees</param>
    /// <param name="lon1">Longitude of first point in degrees</param>
    /// <param name="lat2">Latitude of second point in degrees</param>
    /// <param name="lon2">Longitude of second point in degrees</param>
    /// <returns>Distance in meters</returns>
    public static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        // Convert to radians
        var lat1Rad = DegreesToRadians(lat1);
        var lat2Rad = DegreesToRadians(lat2);
        var deltaLat = DegreesToRadians(lat2 - lat1);
        var deltaLon = DegreesToRadians(lon2 - lon1);

        // Haversine formula
        var a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return EarthRadiusMeters * c;
    }

    /// <summary>
    /// Calculates the speed based on distance traveled and time elapsed.
    /// </summary>
    /// <param name="distanceMeters">Distance traveled in meters</param>
    /// <param name="timeElapsed">Time elapsed during travel</param>
    /// <returns>Speed in meters per second, or null if time is invalid</returns>
    public static double? CalculateSpeed(double distanceMeters, TimeSpan timeElapsed)
    {
        if (timeElapsed.TotalSeconds <= 0)
        {
            return null;
        }

        return distanceMeters / timeElapsed.TotalSeconds;
    }

    /// <summary>
    /// Calculates the speed between two GPS coordinates with timestamps.
    /// </summary>
    /// <param name="lat1">Latitude of first point</param>
    /// <param name="lon1">Longitude of first point</param>
    /// <param name="time1">Timestamp of first point</param>
    /// <param name="lat2">Latitude of second point</param>
    /// <param name="lon2">Longitude of second point</param>
    /// <param name="time2">Timestamp of second point</param>
    /// <returns>Speed in meters per second, or null if time difference is invalid</returns>
    public static double? CalculateSpeed(
        double lat1, double lon1, DateTimeOffset time1,
        double lat2, double lon2, DateTimeOffset time2)
    {
        var distance = CalculateDistance(lat1, lon1, lat2, lon2);
        var timeElapsed = time2 - time1;

        return CalculateSpeed(distance, timeElapsed);
    }

    /// <summary>
    /// Converts speed from meters per second to miles per hour.
    /// </summary>
    public static double MetersPerSecondToMph(double metersPerSecond)
    {
        return metersPerSecond * MpsToMphFactor;
    }

    /// <summary>
    /// Converts speed from meters per second to kilometers per hour.
    /// </summary>
    public static double MetersPerSecondToKmh(double metersPerSecond)
    {
        return metersPerSecond * MpsToKmhFactor;
    }

    /// <summary>
    /// Converts speed from miles per hour to meters per second.
    /// </summary>
    public static double MphToMetersPerSecond(double mph)
    {
        return mph / MpsToMphFactor;
    }

    /// <summary>
    /// Converts speed from kilometers per hour to meters per second.
    /// </summary>
    public static double KmhToMetersPerSecond(double kmh)
    {
        return kmh / MpsToKmhFactor;
    }

    /// <summary>
    /// Determines if the speed indicates walking (< 5 mph / 8 km/h).
    /// </summary>
    public static bool IsWalkingSpeed(double speedMetersPerSecond)
    {
        var speedMph = MetersPerSecondToMph(speedMetersPerSecond);
        return speedMph < 5.0;
    }

    /// <summary>
    /// Determines if the speed indicates riding (>= 5 mph / 8 km/h).
    /// </summary>
    public static bool IsRidingSpeed(double speedMetersPerSecond)
    {
        var speedMph = MetersPerSecondToMph(speedMetersPerSecond);
        return speedMph >= 5.0;
    }

    /// <summary>
    /// Converts degrees to radians.
    /// </summary>
    private static double DegreesToRadians(double degrees)
    {
        return degrees * Math.PI / 180.0;
    }

    /// <summary>
    /// Converts radians to degrees.
    /// </summary>
    public static double RadiansToDegrees(double radians)
    {
        return radians * 180.0 / Math.PI;
    }

    /// <summary>
    /// Calculates the bearing (direction) from one point to another.
    /// </summary>
    /// <param name="lat1">Latitude of first point in degrees</param>
    /// <param name="lon1">Longitude of first point in degrees</param>
    /// <param name="lat2">Latitude of second point in degrees</param>
    /// <param name="lon2">Longitude of second point in degrees</param>
    /// <returns>Bearing in degrees (0-360, where 0/360 is North)</returns>
    public static double CalculateBearing(double lat1, double lon1, double lat2, double lon2)
    {
        var lat1Rad = DegreesToRadians(lat1);
        var lat2Rad = DegreesToRadians(lat2);
        var deltaLon = DegreesToRadians(lon2 - lon1);

        var y = Math.Sin(deltaLon) * Math.Cos(lat2Rad);
        var x = Math.Cos(lat1Rad) * Math.Sin(lat2Rad) -
                Math.Sin(lat1Rad) * Math.Cos(lat2Rad) * Math.Cos(deltaLon);

        var bearing = Math.Atan2(y, x);
        bearing = RadiansToDegrees(bearing);
        bearing = (bearing + 360) % 360;

        return bearing;
    }

    /// <summary>
    /// Checks if a point is within a given radius of another point.
    /// </summary>
    /// <param name="centerLat">Latitude of center point</param>
    /// <param name="centerLon">Longitude of center point</param>
    /// <param name="pointLat">Latitude of point to check</param>
    /// <param name="pointLon">Longitude of point to check</param>
    /// <param name="radiusMeters">Radius in meters</param>
    /// <returns>True if point is within radius</returns>
    public static bool IsWithinRadius(
        double centerLat,
        double centerLon,
        double pointLat,
        double pointLon,
        double radiusMeters)
    {
        var distance = CalculateDistance(centerLat, centerLon, pointLat, pointLon);
        return distance <= radiusMeters;
    }
}
