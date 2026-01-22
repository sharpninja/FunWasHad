using Xunit;
using FWH.Mobile.Services;

namespace FWH.Mobile.Tests.Services;

/// <summary>
/// Tests for GPS distance and bearing calculations.
/// </summary>
public class GpsCalculatorTests
{
    /// <summary>
    /// Tests that distance calculation returns expected values for known coordinate pairs.
    /// </summary>
    /// <param name="lat1">First point latitude in degrees. Used to represent the starting location.</param>
    /// <param name="lon1">First point longitude in degrees. Used to represent the starting location.</param>
    /// <param name="lat2">Second point latitude in degrees. Used to represent the destination location.</param>
    /// <param name="lon2">Second point longitude in degrees. Used to represent the destination location.</param>
    /// <param name="expectedMeters">Expected distance in meters. This is the known correct distance for the coordinate pair, used to validate the Haversine formula implementation.</param>
    /// <remarks>
    /// This test validates the Haversine formula implementation by testing against known distances:
    /// - Same point (0m): Verifies zero distance calculation
    /// - 1 degree at equator (~111.32km): Validates basic distance calculation using Earth's circumference
    /// - NYC landmarks (5.42km): Tests real-world accuracy with actual coordinates
    /// Expected outcome: Distance within 2% tolerance of expected value. This tolerance accounts for floating-point precision and slight variations in Earth's radius calculations.
    /// </remarks>
    [Theory]
    [InlineData(0, 0, 0, 0, 0)] // Same point
    [InlineData(0, 0, 0, 1, 111319.49)] // 1 degree longitude at equator ≈ 111.32 km
    [InlineData(0, 0, 1, 0, 111319.49)] // 1 degree latitude ≈ 111.32 km
    [InlineData(40.7128, -74.0060, 40.7589, -73.9851, 5420.0)] // NYC: Times Square to Central Park (actual distance)
    public void CalculateDistance_WithKnownCoordinates_ReturnsExpectedDistance(
        double lat1, double lon1, double lat2, double lon2, double expectedMeters)
    {
        // Act
        var distance = GpsCalculator.CalculateDistance(lat1, lon1, lat2, lon2);

        // Assert - Allow 2% tolerance for floating point precision and slight calculation variations
        Assert.InRange(distance, expectedMeters * 0.98, expectedMeters * 1.02);
    }

    /// <summary>
    /// Tests that calculating distance between identical coordinates returns zero.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The distance calculation when both points are identical.</para>
    /// <para><strong>Data involved:</strong> San Francisco coordinates (37.7749, -122.4194) used as both start and end point. These coordinates are used because they represent a real-world location, ensuring the test is realistic.</para>
    /// <para><strong>Why the data matters:</strong> Using the same coordinates for both points tests the edge case where no movement occurs, which is common in location tracking when the device hasn't moved.</para>
    /// <para><strong>Expected outcome:</strong> Distance of exactly 0 meters with 5 decimal places precision.</para>
    /// <para><strong>Reason for expectation:</strong> Mathematically, the distance between identical points must be zero. This validates that the Haversine formula correctly handles the edge case and doesn't produce floating-point errors.</para>
    /// </remarks>
    [Fact]
    public void CalculateDistance_WithSamePoint_ReturnsZero()
    {
        // Arrange
        var lat = 37.7749;
        var lon = -122.4194;

        // Act
        var distance = GpsCalculator.CalculateDistance(lat, lon, lat, lon);

        // Assert
        Assert.Equal(0, distance, precision: 5);
    }

    /// <summary>
    /// Tests that bearing calculation returns correct angles for cardinal directions.
    /// </summary>
    /// <param name="lat1">Starting latitude in degrees. Set to 0 (equator) to simplify calculations.</param>
    /// <param name="lon1">Starting longitude in degrees. Set to 0 (prime meridian) to simplify calculations.</param>
    /// <param name="lat2">Destination latitude in degrees. Varies by ±1 degree to test each cardinal direction.</param>
    /// <param name="lon2">Destination longitude in degrees. Varies by ±1 degree to test each cardinal direction.</param>
    /// <param name="expectedBearing">Expected bearing in degrees (0=North, 90=East, 180=South, 270=West). These are standard compass bearings used in navigation.</param>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> Bearing calculation accuracy for the four cardinal directions.</para>
    /// <para><strong>Data involved:</strong> Coordinates at the equator (0,0) as starting point, with destinations 1 degree away in each cardinal direction. Using the equator simplifies calculations and provides clear reference points.</para>
    /// <para><strong>Why the data matters:</strong> Cardinal directions are fundamental to navigation and provide clear, verifiable test cases. Testing at the equator ensures consistent results across all directions.</para>
    /// <para><strong>Expected outcome:</strong> Bearing within 5 degrees of the expected cardinal direction.</para>
    /// <para><strong>Reason for expectation:</strong> The 5-degree tolerance accounts for Earth's curvature and floating-point precision. Cardinal directions should be accurate to within this tolerance for practical navigation use.</para>
    /// </remarks>
    [Theory]
    [InlineData(0, 0, 1, 0, 0)]     // North
    [InlineData(0, 0, 0, 1, 90)]    // East
    [InlineData(0, 0, -1, 0, 180)]  // South
    [InlineData(0, 0, 0, -1, 270)]  // West
    public void CalculateBearing_WithCardinalDirections_ReturnsCorrectBearing(
        double lat1, double lon1, double lat2, double lon2, double expectedBearing)
    {
        // Act
        var bearing = GpsCalculator.CalculateBearing(lat1, lon1, lat2, lon2);

        // Assert - Allow 5 degree tolerance
        Assert.InRange(bearing, expectedBearing - 5, expectedBearing + 5);
    }

    /// <summary>
    /// Tests that radius checking correctly identifies whether points are within a specified distance.
    /// </summary>
    /// <param name="centerLat">Center point latitude in degrees. Represents the reference location for radius calculation.</param>
    /// <param name="centerLon">Center point longitude in degrees. Represents the reference location for radius calculation.</param>
    /// <param name="pointLat">Point latitude in degrees. Represents the location being checked against the radius.</param>
    /// <param name="pointLon">Point longitude in degrees. Represents the location being checked against the radius.</param>
    /// <param name="radiusMeters">Radius in meters. The distance threshold used to determine if the point is within range.</param>
    /// <param name="expectedResult">Expected boolean result (true if within radius, false otherwise). Used to validate the radius check logic.</param>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The IsWithinRadius method's ability to correctly determine if a point falls within a specified circular radius.</para>
    /// <para><strong>Data involved:</strong> Various coordinate pairs at different distances, including edge cases (same point, just within/outside radius). NYC coordinates test real-world scenarios at different scales (1km vs 10km).</para>
    /// <para><strong>Why the data matters:</strong> Radius checking is critical for location-based features like "find nearby businesses". The test data covers edge cases (exact match, boundary conditions) and real-world distances to ensure accuracy.</para>
    /// <para><strong>Expected outcome:</strong> Boolean result matching expectedResult parameter.</para>
    /// <para><strong>Reason for expectation:</strong> The method should correctly apply the Haversine distance formula and compare against the radius threshold. Edge cases (same point, boundary distances) are included to ensure robust behavior.</para>
    /// </remarks>
    [Theory]
    [InlineData(0, 0, 0, 0, 100, true)]      // Same point
    [InlineData(0, 0, 0, 0.0009, 102, true)] // ~100m at equator (actual: ~100.2m, need 102m radius for safety)
    [InlineData(0, 0, 0, 0.002, 100, false)] // ~200m at equator
    [InlineData(40.7128, -74.0060, 40.7589, -73.9851, 10000, true)]  // Within 10km
    [InlineData(40.7128, -74.0060, 40.7589, -73.9851, 1000, false)]  // Not within 1km
    public void IsWithinRadius_WithVariousDistances_ReturnsExpectedResult(
        double centerLat, double centerLon, double pointLat, double pointLon,
        double radiusMeters, bool expectedResult)
    {
        // Act
        var result = GpsCalculator.IsWithinRadius(
            centerLat, centerLon, pointLat, pointLon, radiusMeters);

        // Assert
        Assert.Equal(expectedResult, result);
    }

    /// <summary>
    /// Tests that radians to degrees conversion correctly converts π radians to 180 degrees.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The RadiansToDegrees conversion function using the mathematical constant π.</para>
    /// <para><strong>Data involved:</strong> Math.PI (approximately 3.14159 radians), which represents 180 degrees in the standard conversion formula (degrees = radians × 180 / π).</para>
    /// <para><strong>Why the data matters:</strong> π radians equals exactly 180 degrees, providing a precise reference point for testing the conversion formula. This is a fundamental mathematical relationship used in GPS calculations.</para>
    /// <para><strong>Expected outcome:</strong> Result of exactly 180.0 degrees with 10 decimal places precision.</para>
    /// <para><strong>Reason for expectation:</strong> Mathematically, π radians must equal 180 degrees. The high precision requirement (10 decimal places) ensures the conversion formula is implemented correctly without rounding errors.</para>
    /// </remarks>
    [Fact]
    public void RadiansToDegrees_WithPi_Returns180()
    {
        // Act
        var degrees = GpsCalculator.RadiansToDegrees(Math.PI);

        // Assert
        Assert.Equal(180.0, degrees, precision: 10);
    }

    /// <summary>
    /// Tests that distance calculation between antipodal points (opposite sides of Earth) returns approximately half the Earth's circumference.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> Distance calculation accuracy for the maximum possible distance on Earth (antipodal points).</para>
    /// <para><strong>Data involved:</strong> Coordinates (0°, 0°) and (0°, 180°) representing points on opposite sides of Earth at the equator. Expected distance is 20,015,086 meters (half of Earth's equatorial circumference).</para>
    /// <para><strong>Why the data matters:</strong> Antipodal points represent the maximum distance between two points on Earth's surface. This tests the Haversine formula at its extreme case and validates that the implementation handles the full range of possible distances correctly.</para>
    /// <para><strong>Expected outcome:</strong> Distance within 1% of 20,015,086 meters (between 19,814,935 and 20,215,237 meters).</para>
    /// <para><strong>Reason for expectation:</strong> The 1% tolerance accounts for Earth's oblate spheroid shape (not a perfect sphere) and variations in Earth's radius measurements. The Haversine formula assumes a perfect sphere, so slight deviations are expected for antipodal points.</para>
    /// </remarks>
    [Fact]
    public void CalculateDistance_WithAntipodes_ReturnsHalfEarthCircumference()
    {
        // Arrange - Points on opposite sides of Earth
        var lat1 = 0.0;
        var lon1 = 0.0;
        var lat2 = 0.0;
        var lon2 = 180.0;
        var expectedDistance = 20015086.0; // Half of Earth's circumference at equator

        // Act
        var distance = GpsCalculator.CalculateDistance(lat1, lon1, lat2, lon2);

        // Assert - Allow 1% tolerance
        Assert.InRange(distance, expectedDistance * 0.99, expectedDistance * 1.01);
    }

    /// <summary>
    /// Tests that distance calculation works correctly across different latitudes and hemispheres.
    /// </summary>
    /// <param name="lat1">First point latitude in degrees. Includes locations in Northern (SF, London) and Southern (Sydney) hemispheres.</param>
    /// <param name="lon1">First point longitude in degrees. Represents various global locations to test worldwide compatibility.</param>
    /// <param name="lat2">Second point latitude in degrees. Exactly 0.01 degrees (approximately 1.11km) away from lat1 in the same direction.</param>
    /// <param name="lon2">Second point longitude in degrees. Same as lon1 to test pure latitude-based distance.</param>
    /// <param name="expectedMeters">Expected distance of approximately 1,112-1,113 meters. This represents the distance covered by 0.01 degrees of latitude, which varies slightly by location due to Earth's shape.</param>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> Distance calculation accuracy across different global regions and hemispheres.</para>
    /// <para><strong>Data involved:</strong> Real-world coordinates from San Francisco (37.77°N), London (51.51°N), and Sydney (-33.87°S), each with a point 0.01 degrees north/south. This tests both Northern and Southern hemispheres.</para>
    /// <para><strong>Why the data matters:</strong> Earth's shape (oblate spheroid) means 1 degree of latitude varies slightly by location. Testing multiple regions ensures the Haversine formula works globally, not just at specific latitudes. This is critical for a location-based app used worldwide.</para>
    /// <para><strong>Expected outcome:</strong> Distance within 5% of expected value (1,057-1,168 meters range).</para>
    /// <para><strong>Reason for expectation:</strong> The 5% tolerance accounts for Earth's oblate shape and the fact that 1 degree of latitude is not perfectly constant. The Haversine formula provides good approximations across all tested regions.</para>
    /// </remarks>
    [Theory]
    [InlineData(37.7749, -122.4194, 37.7849, -122.4194, 1113)] // ~1.11km north in SF
    [InlineData(51.5074, -0.1278, 51.5174, -0.1278, 1112)]     // ~1.11km north in London
    [InlineData(-33.8688, 151.2093, -33.8588, 151.2093, 1112)] // ~1.11km south in Sydney
    public void CalculateDistance_WithVariousLatitudes_HandlesAllRegions(
        double lat1, double lon1, double lat2, double lon2, double expectedMeters)
    {
        // Act
        var distance = GpsCalculator.CalculateDistance(lat1, lon1, lat2, lon2);

        // Assert
        Assert.InRange(distance, expectedMeters * 0.95, expectedMeters * 1.05);
    }

    /// <summary>
    /// Tests that distance calculation accurately detects small movements of approximately 50 meters.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> Distance calculation accuracy for small movements, specifically the 50-meter threshold commonly used in location tracking.</para>
    /// <para><strong>Data involved:</strong> Coordinates at the equator (0°, 0°) and a point 0.00045 degrees north, representing approximately 50 meters of movement. The equator is used to simplify calculations and provide consistent results.</para>
    /// <para><strong>Why the data matters:</strong> The 50-meter threshold is a common minimum distance for location tracking updates (to avoid excessive updates when stationary). Accurate detection of this distance is critical for determining when to trigger location updates and movement state changes.</para>
    /// <para><strong>Expected outcome:</strong> Distance between 45 and 55 meters.</para>
    /// <para><strong>Reason for expectation:</strong> The 10% tolerance (45-55m range) accounts for the approximation of 0.00045 degrees and ensures the calculation is accurate enough for practical location tracking. This range is acceptable because it's well within the typical GPS accuracy of ±5-10 meters.</para>
    /// </remarks>
    [Fact]
    public void CalculateDistance_With50MetersMovement_DetectsCorrectly()
    {
        // Arrange - Movement of approximately 50 meters at equator
        var lat1 = 0.0;
        var lon1 = 0.0;
        var lat2 = 0.00045; // ~50 meters
        var lon2 = 0.0;

        // Act
        var distance = GpsCalculator.CalculateDistance(lat1, lon1, lat2, lon2);

        // Assert - Should be close to 50 meters
        Assert.InRange(distance, 45, 55);
    }

    /// <summary>
    /// Tests that distance calculation maintains accuracy for small distances used in location tracking.
    /// </summary>
    /// <param name="lat1">Starting latitude in degrees. Uses equator (0°) for simplicity and NYC (40.71°N) for real-world validation.</param>
    /// <param name="lon1">Starting longitude in degrees. Uses prime meridian (0°) and NYC (-74.01°W) for testing.</param>
    /// <param name="lat2">Destination latitude in degrees. Varies from 0.00045° to 0.0045° to represent 50m, 100m, and 500m movements.</param>
    /// <param name="lon2">Destination longitude in degrees. Same as lon1 to test pure latitude-based movement.</param>
    /// <param name="expectedMeters">Expected distance in meters (50, 100, 500, 55). These represent common movement thresholds in location tracking applications.</param>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> Distance calculation precision for small distances (50-500 meters) that are critical for location tracking accuracy.</para>
    /// <para><strong>Data involved:</strong> Small coordinate differences representing typical pedestrian movements (50-500m). Includes both equator-based calculations and a real-world NYC example to validate accuracy across latitudes.</para>
    /// <para><strong>Why the data matters:</strong> Small distances are the most common in location tracking (walking, stationary detection). Inaccurate calculations here would cause false movement detections or missed location updates, leading to poor user experience.</para>
    /// <para><strong>Expected outcome:</strong> Distance within 10% of expected value (e.g., 45-55m for 50m expected).</para>
    /// <para><strong>Reason for expectation:</strong> The 10% tolerance is appropriate for small distances where GPS accuracy (±5-10m) and coordinate precision limitations exist. This ensures the calculation is accurate enough for practical use while accounting for real-world measurement uncertainties.</para>
    /// </remarks>
    [Theory]
    [InlineData(0, 0, 0.00045, 0, 50)]       // ~50m north
    [InlineData(0, 0, 0.0009, 0, 100)]       // ~100m north
    [InlineData(0, 0, 0.0045, 0, 500)]       // ~500m north
    [InlineData(40.7128, -74.0060, 40.7133, -74.0060, 55)] // ~55m north in NYC
    public void CalculateDistance_WithSmallDistances_IsAccurate(
        double lat1, double lon1, double lat2, double lon2, double expectedMeters)
    {
        // Act
        var distance = GpsCalculator.CalculateDistance(lat1, lon1, lat2, lon2);

        // Assert - Allow 10% tolerance for small distances
        Assert.InRange(distance, expectedMeters * 0.9, expectedMeters * 1.1);
    }
}

/// <summary>
/// Tests for speed calculations.
/// </summary>
public class SpeedCalculationTests
{
    /// <summary>
    /// Tests that speed calculation correctly computes meters per second from distance and time.
    /// </summary>
    /// <param name="distanceMeters">Distance traveled in meters. Includes various distances (50m, 100m, 1000m, 1609.34m) to test different scales.</param>
    /// <param name="timeSeconds">Time elapsed in seconds. Ranges from 5 seconds to 3600 seconds (1 hour) to test various time scales.</param>
    /// <param name="expectedMetersPerSecond">Expected speed in meters per second. All test cases result in 10 m/s except the last (1 mph = 0.447 m/s), providing both common and edge case validation.</param>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The CalculateSpeed method's ability to correctly compute speed using the formula: speed = distance / time.</para>
    /// <para><strong>Data involved:</strong> Various distance-time combinations that all result in 10 m/s (except 1 mph case). Includes 1 mile in 1 hour (1 mph) to validate conversion accuracy. The 1609.34 meters represents exactly 1 mile.</para>
    /// <para><strong>Why the data matters:</strong> Speed calculation is essential for movement state detection (walking vs riding). The test data covers different scales (short distances, long distances, different time periods) to ensure the formula works correctly across all scenarios. The 1 mph case validates conversion from imperial to metric units.</para>
    /// <para><strong>Expected outcome:</strong> Calculated speed within 1% of expected value (e.g., 9.9-10.1 m/s for 10 m/s expected).</para>
    /// <para><strong>Reason for expectation:</strong> The 1% tolerance accounts for floating-point precision in division operations. Speed calculations must be accurate for movement state detection, where the 5 mph threshold (2.24 m/s) requires precise calculations.</para>
    /// </remarks>
    [Theory]
    [InlineData(100, 10, 10.0)]      // 100m in 10s = 10 m/s
    [InlineData(1000, 100, 10.0)]    // 1000m in 100s = 10 m/s
    [InlineData(50, 5, 10.0)]        // 50m in 5s = 10 m/s
    [InlineData(1609.34, 3600, 0.447)] // 1 mile in 1 hour ≈ 0.447 m/s (1 mph)
    public void CalculateSpeed_WithDistanceAndTime_ReturnsCorrectSpeed(
        double distanceMeters, double timeSeconds, double expectedMetersPerSecond)
    {
        // Arrange
        var timeElapsed = TimeSpan.FromSeconds(timeSeconds);

        // Act
        var speed = GpsCalculator.CalculateSpeed(distanceMeters, timeElapsed);

        // Assert
        Assert.NotNull(speed);
        Assert.InRange(speed.Value, expectedMetersPerSecond * 0.99, expectedMetersPerSecond * 1.01);
    }

    /// <summary>
    /// Tests that speed calculation returns null when time is zero to prevent division by zero.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The CalculateSpeed method's handling of the edge case where time elapsed is zero.</para>
    /// <para><strong>Data involved:</strong> Distance of 100 meters with TimeSpan.Zero (0 seconds). The 100 meters is arbitrary; any distance with zero time should return null.</para>
    /// <para><strong>Why the data matters:</strong> Division by zero would cause an exception or produce invalid results (infinity). This edge case can occur in real scenarios when GPS timestamps are identical or when location updates happen simultaneously. Returning null is the correct behavior to indicate speed cannot be calculated.</para>
    /// <para><strong>Expected outcome:</strong> Returns null (no exception thrown).</para>
    /// <para><strong>Reason for expectation:</strong> Mathematically, speed cannot be calculated when time is zero (division by zero is undefined). Returning null allows calling code to handle this gracefully rather than throwing an exception, which is better for location tracking services that may encounter this edge case.</para>
    /// </remarks>
    [Fact]
    public void CalculateSpeed_WithZeroTime_ReturnsNull()
    {
        // Act
        var speed = GpsCalculator.CalculateSpeed(100, TimeSpan.Zero);

        // Assert
        Assert.Null(speed);
    }

    /// <summary>
    /// Tests that speed calculation returns null when time is negative to prevent invalid calculations.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The CalculateSpeed method's handling of invalid time values (negative time).</para>
    /// <para><strong>Data involved:</strong> Distance of 100 meters with TimeSpan.FromSeconds(-10), representing a negative 10-second time interval. Negative time is physically impossible and indicates data corruption or clock synchronization issues.</para>
    /// <para><strong>Why the data matters:</strong> Negative time can occur due to GPS clock errors, system clock adjustments, or data corruption. Calculating speed with negative time would produce negative speed values, which are meaningless. The method must detect and reject this invalid input.</para>
    /// <para><strong>Expected outcome:</strong> Returns null (no exception thrown).</para>
    /// <para><strong>Reason for expectation:</strong> Negative time is invalid input that cannot produce a meaningful speed calculation. Returning null allows the calling code to handle the error gracefully, which is preferable to throwing an exception in a location tracking service that processes continuous GPS data streams.</para>
    /// </remarks>
    [Fact]
    public void CalculateSpeed_WithNegativeTime_ReturnsNull()
    {
        // Act
        var speed = GpsCalculator.CalculateSpeed(100, TimeSpan.FromSeconds(-10));

        // Assert
        Assert.Null(speed);
    }

    /// <summary>
    /// Tests that speed calculation using GPS coordinates and timestamps returns correct speed.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The CalculateSpeed overload that takes GPS coordinates and timestamps, combining distance and time calculations.</para>
    /// <para><strong>Data involved:</strong> Two GPS coordinates at the equator: (0°, 0°) and (0.0009°, 0°), representing approximately 100 meters of movement. Time difference of 10 seconds between the two points. Using the equator simplifies distance calculations.</para>
    /// <para><strong>Why the data matters:</strong> This tests the complete speed calculation workflow: distance calculation from coordinates, time difference from timestamps, and speed computation. This is the primary method used in location tracking services to determine movement speed from GPS updates.</para>
    /// <para><strong>Expected outcome:</strong> Speed between 9.0 and 11.0 meters per second (approximately 10 m/s).</para>
    /// <para><strong>Reason for expectation:</strong> The 10% tolerance (9-11 m/s range) accounts for the approximation of 0.0009 degrees representing 100 meters and ensures the calculation is accurate enough for movement state detection. This range is acceptable because it's well above the 2.24 m/s (5 mph) threshold used for walking/riding detection.</para>
    /// </remarks>
    [Fact]
    public void CalculateSpeed_WithGpsCoordinates_ReturnsCorrectSpeed()
    {
        // Arrange - 100m movement in 10 seconds
        var lat1 = 0.0;
        var lon1 = 0.0;
        var time1 = DateTimeOffset.UtcNow;

        var lat2 = 0.0009; // ~100m
        var lon2 = 0.0;
        var time2 = time1.AddSeconds(10);

        // Act
        var speed = GpsCalculator.CalculateSpeed(lat1, lon1, time1, lat2, lon2, time2);

        // Assert
        Assert.NotNull(speed);
        Assert.InRange(speed.Value, 9.0, 11.0); // ~10 m/s with tolerance
    }

    /// <summary>
    /// Tests that conversion from meters per second to miles per hour returns correct values.
    /// </summary>
    /// <param name="metersPerSecond">Speed in meters per second. Includes common speeds: 2.24 m/s (5 mph walking threshold), 4.47 m/s (10 mph), 13.41 m/s (30 mph), 26.82 m/s (60 mph).</param>
    /// <param name="expectedMph">Expected speed in miles per hour. These values represent common speed thresholds used in movement state detection and user display.</param>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The MetersPerSecondToMph conversion function using the formula: mph = m/s × 2.23694.</para>
    /// <para><strong>Data involved:</strong> Various speeds in m/s covering the range from walking (5 mph) to highway speeds (60 mph). The 5 mph case (2.23694 m/s) is particularly important as it's the threshold for walking vs riding detection.</para>
    /// <para><strong>Why the data matters:</strong> Speed conversion is needed for user display (showing mph) and for movement state detection (5 mph threshold). Accurate conversion is critical because the 5 mph threshold determines whether the user is walking or riding, affecting workflow behavior.</para>
    /// <para><strong>Expected outcome:</strong> Converted speed within 1% of expected mph value.</para>
    /// <para><strong>Reason for expectation:</strong> The 1% tolerance accounts for floating-point precision in multiplication operations. The conversion must be accurate because the 5 mph threshold (2.23694 m/s) is used for critical movement state decisions.</para>
    /// </remarks>
    [Theory]
    [InlineData(2.23694, 5.0)]   // 2.23694 m/s = 5 mph
    [InlineData(4.4739, 10.0)]   // 4.4739 m/s = 10 mph
    [InlineData(13.4112, 30.0)]  // 13.4112 m/s = 30 mph
    [InlineData(26.8224, 60.0)]  // 26.8224 m/s = 60 mph
    public void MetersPerSecondToMph_ConvertsCorrectly(double metersPerSecond, double expectedMph)
    {
        // Act
        var mph = GpsCalculator.MetersPerSecondToMph(metersPerSecond);

        // Assert
        Assert.InRange(mph, expectedMph * 0.99, expectedMph * 1.01);
    }

    /// <summary>
    /// Tests that conversion from meters per second to kilometers per hour returns correct values.
    /// </summary>
    /// <param name="metersPerSecond">Speed in meters per second. Includes 3.6 m/s (13 km/h), 10 m/s (36 km/h), and 27.778 m/s (100 km/h) to test various speed ranges.</param>
    /// <param name="expectedKmh">Expected speed in kilometers per hour. These values are used for international users who prefer metric units.</param>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The MetersPerSecondToKmh conversion function using the formula: km/h = m/s × 3.6.</para>
    /// <para><strong>Data involved:</strong> Various speeds covering walking to highway speeds. The 3.6 m/s case represents a common walking speed, while 100 km/h represents highway driving.</para>
    /// <para><strong>Why the data matters:</strong> Speed conversion to km/h is needed for international users and for displaying speeds in metric units. Accurate conversion ensures consistent user experience across different unit preferences.</para>
    /// <para><strong>Expected outcome:</strong> Converted speed within 1% of expected km/h value.</para>
    /// <para><strong>Reason for expectation:</strong> The 1% tolerance accounts for floating-point precision. The conversion formula (multiply by 3.6) is straightforward and should produce accurate results within this tolerance.</para>
    /// </remarks>
    [Theory]
    [InlineData(3.6, 13.0)]      // 3.6 m/s = 13 km/h
    [InlineData(10.0, 36.0)]     // 10 m/s = 36 km/h
    [InlineData(27.778, 100.0)]  // 27.778 m/s = 100 km/h
    public void MetersPerSecondToKmh_ConvertsCorrectly(double metersPerSecond, double expectedKmh)
    {
        // Act
        var kmh = GpsCalculator.MetersPerSecondToKmh(metersPerSecond);

        // Assert
        Assert.InRange(kmh, expectedKmh * 0.99, expectedKmh * 1.01);
    }

    /// <summary>
    /// Tests that conversion from miles per hour to meters per second returns correct values.
    /// </summary>
    /// <param name="mph">Speed in miles per hour. Includes 5 mph (walking threshold), 10 mph, and 60 mph (highway speed) to test critical and common speeds.</param>
    /// <param name="expectedMetersPerSecond">Expected speed in meters per second. The 5 mph case (2.23694 m/s) is critical as it's the threshold for movement state detection.</param>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The MphToMetersPerSecond conversion function using the formula: m/s = mph / 2.23694.</para>
    /// <para><strong>Data involved:</strong> Common speeds in mph: 5 mph (walking/riding threshold), 10 mph, and 60 mph. These represent the range from walking to highway driving speeds.</para>
    /// <para><strong>Why the data matters:</strong> This conversion is needed when speed thresholds are specified in mph (like the 5 mph walking/riding threshold) but internal calculations use m/s. The 5 mph case is particularly critical as it determines movement state transitions.</para>
    /// <para><strong>Expected outcome:</strong> Converted speed within 1% of expected m/s value.</para>
    /// <para><strong>Reason for expectation:</strong> The 1% tolerance accounts for floating-point precision in division operations. Accurate conversion is essential because the 5 mph threshold (2.23694 m/s) is used for critical movement state decisions that affect workflow behavior.</para>
    /// </remarks>
    [Theory]
    [InlineData(5.0, 2.23694)]   // 5 mph = 2.23694 m/s
    [InlineData(10.0, 4.4739)]   // 10 mph = 4.4739 m/s
    [InlineData(60.0, 26.8224)]  // 60 mph = 26.8224 m/s
    public void MphToMetersPerSecond_ConvertsCorrectly(double mph, double expectedMetersPerSecond)
    {
        // Act
        var metersPerSecond = GpsCalculator.MphToMetersPerSecond(mph);

        // Assert
        Assert.InRange(metersPerSecond, expectedMetersPerSecond * 0.99, expectedMetersPerSecond * 1.01);
    }

    /// <summary>
    /// Tests that conversion from kilometers per hour to meters per second returns correct values.
    /// </summary>
    /// <param name="kmh">Speed in kilometers per hour. Includes 13 km/h (walking), 36 km/h, and 100 km/h (highway) to test various speed ranges.</param>
    /// <param name="expectedMetersPerSecond">Expected speed in meters per second. Used for internal calculations when user input or configuration is in km/h.</param>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The KmhToMetersPerSecond conversion function using the formula: m/s = km/h / 3.6.</para>
    /// <para><strong>Data involved:</strong> Common speeds in km/h covering walking to highway speeds. These represent typical speeds users might configure or see in the application.</para>
    /// <para><strong>Why the data matters:</strong> This conversion is needed when speed thresholds are specified in km/h (for international users) but internal calculations use m/s. Accurate conversion ensures consistent behavior regardless of the input unit.</para>
    /// <para><strong>Expected outcome:</strong> Converted speed within 1% of expected m/s value.</para>
    /// <para><strong>Reason for expectation:</strong> The 1% tolerance accounts for floating-point precision. The conversion formula (divide by 3.6) is straightforward and should produce accurate results within this tolerance.</para>
    /// </remarks>
    [Theory]
    [InlineData(13.0, 3.6)]      // 13 km/h = 3.6 m/s
    [InlineData(36.0, 10.0)]     // 36 km/h = 10 m/s
    [InlineData(100.0, 27.778)]  // 100 km/h = 27.778 m/s
    public void KmhToMetersPerSecond_ConvertsCorrectly(double kmh, double expectedMetersPerSecond)
    {
        // Act
        var metersPerSecond = GpsCalculator.KmhToMetersPerSecond(kmh);

        // Assert
        Assert.InRange(metersPerSecond, expectedMetersPerSecond * 0.99, expectedMetersPerSecond * 1.01);
    }

    /// <summary>
    /// Tests that IsWalkingSpeed correctly identifies speeds below the 5 mph threshold as walking.
    /// </summary>
    /// <param name="metersPerSecond">Speed in meters per second. Ranges from 0.5 m/s (1.1 mph) to 2.23 m/s (4.99 mph), all below the 5 mph (2.23694 m/s) threshold.</param>
    /// <param name="expected">Expected result (always true for this test). All speeds below 5 mph should be classified as walking.</param>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The IsWalkingSpeed method's ability to correctly identify walking speeds (below 5 mph threshold).</para>
    /// <para><strong>Data involved:</strong> Various speeds from 0.5 m/s to 2.23 m/s, representing typical walking speeds (1-5 mph). The 2.23 m/s case (4.99 mph) tests the boundary condition just below the threshold.</para>
    /// <para><strong>Why the data matters:</strong> Accurate walking speed detection is critical for movement state classification. The 5 mph threshold determines whether the user is walking or riding, which affects workflow behavior and location tracking logic. Testing speeds just below the threshold ensures the boundary condition is handled correctly.</para>
    /// <para><strong>Expected outcome:</strong> Returns true for all test cases.</para>
    /// <para><strong>Reason for expectation:</strong> All test speeds are below 5 mph (2.23694 m/s), so they should all be classified as walking. The 2.23 m/s case (4.99 mph) is particularly important as it tests the boundary condition - it must return true because it's just below the threshold.</para>
    /// </remarks>
    [Theory]
    [InlineData(0.5, true)]      // 1.1 mph - walking
    [InlineData(1.0, true)]      // 2.2 mph - walking
    [InlineData(1.5, true)]      // 3.4 mph - walking
    [InlineData(2.0, true)]      // 4.5 mph - walking
    [InlineData(2.23, true)]     // 4.99 mph - walking (just under threshold)
    public void IsWalkingSpeed_WithSpeedsBelowThreshold_ReturnsTrue(double metersPerSecond, bool expected)
    {
        // Act
        var result = GpsCalculator.IsWalkingSpeed(metersPerSecond);

        // Assert
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// Tests that IsWalkingSpeed correctly identifies speeds at or above the 5 mph threshold as not walking.
    /// </summary>
    /// <param name="metersPerSecond">Speed in meters per second. Ranges from 2.24 m/s (5.01 mph, just above threshold) to 26.8 m/s (60 mph), all at or above the 5 mph threshold.</param>
    /// <param name="expected">Expected result (always false for this test). All speeds at or above 5 mph should not be classified as walking.</param>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The IsWalkingSpeed method's ability to correctly reject riding speeds (at or above 5 mph threshold).</para>
    /// <para><strong>Data involved:</strong> Various speeds from 2.24 m/s (5.01 mph, just above threshold) to 26.8 m/s (60 mph), representing typical riding/driving speeds. The 2.24 m/s case tests the boundary condition just above the threshold.</para>
    /// <para><strong>Why the data matters:</strong> Accurate rejection of riding speeds is critical for movement state classification. The 5 mph threshold must be strictly enforced - speeds at or above 5 mph should not be classified as walking. The 2.24 m/s case (5.01 mph) is particularly important as it tests the boundary condition.</para>
    /// <para><strong>Expected outcome:</strong> Returns false for all test cases.</para>
    /// <para><strong>Reason for expectation:</strong> All test speeds are at or above 5 mph (2.23694 m/s), so they should all return false (not walking). The 2.24 m/s case (5.01 mph) must return false because it's at the threshold - the threshold is exclusive for walking (walking is strictly less than 5 mph).</para>
    /// </remarks>
    [Theory]
    [InlineData(2.24, false)]    // 5.01 mph - riding (just above threshold)
    [InlineData(4.5, false)]     // 10.1 mph - riding
    [InlineData(10.0, false)]    // 22.4 mph - riding
    [InlineData(20.0, false)]    // 44.7 mph - riding
    [InlineData(26.8, false)]    // 60 mph - riding
    public void IsWalkingSpeed_WithSpeedsAboveThreshold_ReturnsFalse(double metersPerSecond, bool expected)
    {
        // Act
        var result = GpsCalculator.IsWalkingSpeed(metersPerSecond);

        // Assert
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// Tests that IsRidingSpeed correctly identifies speeds at or above the 5 mph threshold as riding.
    /// </summary>
    /// <param name="metersPerSecond">Speed in meters per second. Ranges from 2.24 m/s (5.01 mph) to 20.0 m/s (44.7 mph), all at or above the 5 mph threshold.</param>
    /// <param name="expected">Expected result (always true for this test). All speeds at or above 5 mph should be classified as riding.</param>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The IsRidingSpeed method's ability to correctly identify riding speeds (at or above 5 mph threshold).</para>
    /// <para><strong>Data involved:</strong> Various speeds from 2.24 m/s (5.01 mph, just above threshold) to 20.0 m/s (44.7 mph), representing typical riding/driving speeds. The 2.24 m/s case tests the boundary condition.</para>
    /// <para><strong>Why the data matters:</strong> Accurate riding speed detection is critical for movement state classification. The 5 mph threshold determines whether the user is riding, which affects workflow behavior. Testing speeds just above the threshold ensures the boundary condition is handled correctly.</para>
    /// <para><strong>Expected outcome:</strong> Returns true for all test cases.</para>
    /// <para><strong>Reason for expectation:</strong> All test speeds are at or above 5 mph (2.23694 m/s), so they should all be classified as riding. The 2.24 m/s case (5.01 mph) is particularly important as it tests the boundary condition - it must return true because it's at or above the threshold.</para>
    /// </remarks>
    [Theory]
    [InlineData(2.24, true)]     // 5.01 mph - riding
    [InlineData(4.5, true)]      // 10.1 mph - riding
    [InlineData(10.0, true)]     // 22.4 mph - riding
    [InlineData(20.0, true)]     // 44.7 mph - riding
    public void IsRidingSpeed_WithSpeedsAboveThreshold_ReturnsTrue(double metersPerSecond, bool expected)
    {
        // Act
        var result = GpsCalculator.IsRidingSpeed(metersPerSecond);

        // Assert
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// Tests that IsRidingSpeed correctly identifies speeds below the 5 mph threshold as not riding.
    /// </summary>
    /// <param name="metersPerSecond">Speed in meters per second. Ranges from 0.5 m/s (1.1 mph) to 2.23 m/s (4.99 mph), all below the 5 mph threshold.</param>
    /// <param name="expected">Expected result (always false for this test). All speeds below 5 mph should not be classified as riding.</param>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The IsRidingSpeed method's ability to correctly reject walking speeds (below 5 mph threshold).</para>
    /// <para><strong>Data involved:</strong> Various speeds from 0.5 m/s to 2.23 m/s, representing typical walking speeds (1-5 mph). The 2.23 m/s case (4.99 mph) tests the boundary condition just below the threshold.</para>
    /// <para><strong>Why the data matters:</strong> Accurate rejection of walking speeds is critical for movement state classification. The 5 mph threshold must be strictly enforced - speeds below 5 mph should not be classified as riding. The 2.23 m/s case (4.99 mph) is particularly important as it tests the boundary condition.</para>
    /// <para><strong>Expected outcome:</strong> Returns false for all test cases.</para>
    /// <para><strong>Reason for expectation:</strong> All test speeds are below 5 mph (2.23694 m/s), so they should all return false (not riding). The 2.23 m/s case (4.99 mph) must return false because it's below the threshold - the threshold is inclusive for riding (riding is at or above 5 mph).</para>
    /// </remarks>
    [Theory]
    [InlineData(0.5, false)]     // 1.1 mph - walking
    [InlineData(1.0, false)]     // 2.2 mph - walking
    [InlineData(2.0, false)]     // 4.5 mph - walking
    [InlineData(2.23, false)]    // 4.99 mph - walking
    public void IsRidingSpeed_WithSpeedsBelowThreshold_ReturnsFalse(double metersPerSecond, bool expected)
    {
        // Act
        var result = GpsCalculator.IsRidingSpeed(metersPerSecond);

        // Assert
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// Tests that IsWalkingSpeed returns false when speed is exactly 5 mph (the threshold).
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The IsWalkingSpeed method's behavior at the exact 5 mph threshold boundary.</para>
    /// <para><strong>Data involved:</strong> Speed of exactly 5.0 mph, converted to 2.23694 m/s using MphToMetersPerSecond. This represents the precise threshold value used for walking/riding classification.</para>
    /// <para><strong>Why the data matters:</strong> The 5 mph threshold is the critical boundary between walking and riding states. Testing the exact threshold value ensures the boundary condition is handled correctly. At exactly 5 mph, the speed should be classified as riding (not walking), making the threshold exclusive for walking.</para>
    /// <para><strong>Expected outcome:</strong> Returns false (not walking).</para>
    /// <para><strong>Reason for expectation:</strong> The threshold is defined as "walking is strictly less than 5 mph". At exactly 5 mph, the speed should be classified as riding, not walking. This ensures clear separation between walking and riding states and prevents ambiguous classifications at the boundary.</para>
    /// </remarks>
    [Fact]
    public void IsWalkingSpeed_AtExactly5Mph_ReturnsFalse()
    {
        // Arrange - Exactly 5 mph = 2.23694 m/s
        var speed = GpsCalculator.MphToMetersPerSecond(5.0);

        // Act
        var result = GpsCalculator.IsWalkingSpeed(speed);

        // Assert - At exactly 5 mph, should be riding, not walking
        Assert.False(result);
    }

    /// <summary>
    /// Tests that IsRidingSpeed returns true when speed is exactly 5 mph (the threshold).
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The IsRidingSpeed method's behavior at the exact 5 mph threshold boundary.</para>
    /// <para><strong>Data involved:</strong> Speed of exactly 5.0 mph, converted to 2.23694 m/s using MphToMetersPerSecond. This represents the precise threshold value used for walking/riding classification.</para>
    /// <para><strong>Why the data matters:</strong> The 5 mph threshold is the critical boundary between walking and riding states. Testing the exact threshold value ensures the boundary condition is handled correctly. At exactly 5 mph, the speed should be classified as riding, making the threshold inclusive for riding.</para>
    /// <para><strong>Expected outcome:</strong> Returns true (riding).</para>
    /// <para><strong>Reason for expectation:</strong> The threshold is defined as "riding is at or above 5 mph". At exactly 5 mph, the speed should be classified as riding. This ensures clear separation between walking and riding states and provides consistent behavior with IsWalkingSpeed (which returns false at exactly 5 mph).</para>
    /// </remarks>
    [Fact]
    public void IsRidingSpeed_AtExactly5Mph_ReturnsTrue()
    {
        // Arrange - Exactly 5 mph = 2.23694 m/s
        var speed = GpsCalculator.MphToMetersPerSecond(5.0);

        // Act
        var result = GpsCalculator.IsRidingSpeed(speed);

        // Assert - At exactly 5 mph, should be riding
        Assert.True(result);
    }
}
