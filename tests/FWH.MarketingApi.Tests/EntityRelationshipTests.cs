using FWH.MarketingApi.Data;
using FWH.MarketingApi.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FWH.MarketingApi.Tests;

/// <summary>
/// Tests for entity relationships in the Marketing API.
/// Tests many-to-many relationships between City-TourismMarket and Airport-TourismMarket.
/// </summary>
public class EntityRelationshipTests : ControllerTestBase
{
    public EntityRelationshipTests()
    {
        SeedTestData();
    }

    private void SeedTestData()
    {
        // Clear existing data - delete in proper order to respect foreign keys
        DbContext.CityTourismMarkets.RemoveRange(DbContext.CityTourismMarkets);
        DbContext.AirportTourismMarkets.RemoveRange(DbContext.AirportTourismMarkets);
        DbContext.CityThemes.RemoveRange(DbContext.CityThemes);
        DbContext.Cities.RemoveRange(DbContext.Cities);
        DbContext.Airports.RemoveRange(DbContext.Airports);
        DbContext.TourismMarkets.RemoveRange(DbContext.TourismMarkets);
        DbContext.SaveChanges();

        var now = DateTimeOffset.UtcNow;

        // Create tourism markets
        var coastalMarket = new TourismMarket
        {
            Id = 1,
            Name = "Coastal Tourism",
            Description = "Coastal destinations",
            IsActive = true,
            CreatedAt = now
        };

        var mountainMarket = new TourismMarket
        {
            Id = 2,
            Name = "Mountain Tourism",
            Description = "Mountain destinations",
            IsActive = true,
            CreatedAt = now
        };

        DbContext.TourismMarkets.AddRange(coastalMarket, mountainMarket);

        // Create cities
        var sanFrancisco = new City
        {
            Id = 1,
            Name = "San Francisco",
            State = "California",
            Country = "USA",
            Latitude = 37.7749,
            Longitude = -122.4194,
            IsActive = true,
            CreatedAt = now
        };

        var seattle = new City
        {
            Id = 2,
            Name = "Seattle",
            State = "Washington",
            Country = "USA",
            Latitude = 47.6062,
            Longitude = -122.3321,
            IsActive = true,
            CreatedAt = now
        };

        DbContext.Cities.AddRange(sanFrancisco, seattle);

        // Create city-tourism market relationships
        var sfCoastal = new CityTourismMarket
        {
            Id = 1,
            CityId = 1,
            TourismMarketId = 1,
            City = sanFrancisco,
            TourismMarket = coastalMarket,
            CreatedAt = now
        };

        var seattleMountain = new CityTourismMarket
        {
            Id = 2,
            CityId = 2,
            TourismMarketId = 2,
            City = seattle,
            TourismMarket = mountainMarket,
            CreatedAt = now
        };

        // San Francisco is in both markets
        var sfMountain = new CityTourismMarket
        {
            Id = 3,
            CityId = 1,
            TourismMarketId = 2,
            City = sanFrancisco,
            TourismMarket = mountainMarket,
            CreatedAt = now
        };

        DbContext.CityTourismMarkets.AddRange(sfCoastal, seattleMountain, sfMountain);

        // Create airports
        var sfo = new Airport
        {
            Id = 1,
            Name = "San Francisco International Airport",
            IataCode = "SFO",
            IcaoCode = "KSFO",
            City = "San Francisco",
            State = "California",
            Country = "USA",
            Latitude = 37.6213,
            Longitude = -122.3790,
            IsActive = true,
            CreatedAt = now
        };

        var sea = new Airport
        {
            Id = 2,
            Name = "Seattle-Tacoma International Airport",
            IataCode = "SEA",
            IcaoCode = "KSEA",
            City = "Seattle",
            State = "Washington",
            Country = "USA",
            Latitude = 47.4502,
            Longitude = -122.3088,
            IsActive = true,
            CreatedAt = now
        };

        DbContext.Airports.AddRange(sfo, sea);

        // Create airport-tourism market relationships
        var sfoCoastal = new AirportTourismMarket
        {
            Id = 1,
            AirportId = 1,
            TourismMarketId = 1,
            Airport = sfo,
            TourismMarket = coastalMarket,
            CreatedAt = now
        };

        var seaMountain = new AirportTourismMarket
        {
            Id = 2,
            AirportId = 2,
            TourismMarketId = 2,
            Airport = sea,
            TourismMarket = mountainMarket,
            CreatedAt = now
        };

        // SFO is in both markets
        var sfoMountain = new AirportTourismMarket
        {
            Id = 3,
            AirportId = 1,
            TourismMarketId = 2,
            Airport = sfo,
            TourismMarket = mountainMarket,
            CreatedAt = now
        };

        DbContext.AirportTourismMarkets.AddRange(sfoCoastal, seaMountain, sfoMountain);

        DbContext.SaveChanges();
    }

    /// <summary>
    /// Tests that a city can be associated with multiple tourism markets.
    /// </summary>
    [Fact]
    public async Task CityCanBeInMultipleTourismMarkets()
    {
        var city = await DbContext.Cities
            .Include(c => c.CityTourismMarkets)
                .ThenInclude(ctm => ctm.TourismMarket)
            .FirstOrDefaultAsync(c => c.Id == 1).ConfigureAwait(false);

        Assert.NotNull(city);
        Assert.Equal("San Francisco", city.Name);
        Assert.Equal(2, city.CityTourismMarkets.Count);
        Assert.Contains(city.CityTourismMarkets, ctm => ctm.TourismMarket.Name == "Coastal Tourism");
        Assert.Contains(city.CityTourismMarkets, ctm => ctm.TourismMarket.Name == "Mountain Tourism");
    }

    /// <summary>
    /// Tests that a tourism market can contain multiple cities.
    /// </summary>
    [Fact]
    public async Task TourismMarketCanContainMultipleCities()
    {
        var market = await DbContext.TourismMarkets
            .Include(tm => tm.CityTourismMarkets)
                .ThenInclude(ctm => ctm.City)
            .FirstOrDefaultAsync(tm => tm.Id == 2).ConfigureAwait(false); // Mountain Tourism

        Assert.NotNull(market);
        Assert.Equal("Mountain Tourism", market.Name);
        Assert.Equal(2, market.CityTourismMarkets.Count);
        Assert.Contains(market.CityTourismMarkets, ctm => ctm.City.Name == "San Francisco");
        Assert.Contains(market.CityTourismMarkets, ctm => ctm.City.Name == "Seattle");
    }

    /// <summary>
    /// Tests that an airport can be associated with multiple tourism markets.
    /// </summary>
    [Fact]
    public async Task AirportCanBeInMultipleTourismMarkets()
    {
        var airport = await DbContext.Airports
            .Include(a => a.AirportTourismMarkets)
                .ThenInclude(atm => atm.TourismMarket)
            .FirstOrDefaultAsync(a => a.Id == 1).ConfigureAwait(false);

        Assert.NotNull(airport);
        Assert.Equal("SFO", airport.IataCode);
        Assert.Equal(2, airport.AirportTourismMarkets.Count);
        Assert.Contains(airport.AirportTourismMarkets, atm => atm.TourismMarket.Name == "Coastal Tourism");
        Assert.Contains(airport.AirportTourismMarkets, atm => atm.TourismMarket.Name == "Mountain Tourism");
    }

    /// <summary>
    /// Tests that a tourism market can contain multiple airports.
    /// </summary>
    [Fact]
    public async Task TourismMarketCanContainMultipleAirports()
    {
        var market = await DbContext.TourismMarkets
            .Include(tm => tm.AirportTourismMarkets)
                .ThenInclude(atm => atm.Airport)
            .FirstOrDefaultAsync(tm => tm.Id == 2).ConfigureAwait(false); // Mountain Tourism

        Assert.NotNull(market);
        Assert.Equal("Mountain Tourism", market.Name);
        Assert.Equal(2, market.AirportTourismMarkets.Count);
        Assert.Contains(market.AirportTourismMarkets, atm => atm.Airport.IataCode == "SFO");
        Assert.Contains(market.AirportTourismMarkets, atm => atm.Airport.IataCode == "SEA");
    }

    /// <summary>
    /// Tests that a city can have zero tourism markets (optional relationship).
    /// </summary>
    [Fact]
    public async Task CityCanHaveZeroTourismMarkets()
    {
        // Create a city without any tourism markets
        // Use a unique name to avoid conflicts with other tests
        var uniqueCityName = $"TestCity_{Guid.NewGuid():N}";
        var city = new City
        {
            Name = uniqueCityName,
            State = "Oregon",
            Country = "USA",
            Latitude = 45.5152,
            Longitude = -122.6784,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        DbContext.Cities.Add(city);
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        var retrievedCity = await DbContext.Cities
            .Include(c => c.CityTourismMarkets)
            .FirstOrDefaultAsync(c => c.Name == uniqueCityName).ConfigureAwait(false);

        Assert.NotNull(retrievedCity);
        Assert.Empty(retrievedCity.CityTourismMarkets);
    }

    /// <summary>
    /// Tests that an airport can have zero tourism markets (optional relationship).
    /// </summary>
    [Fact]
    public async Task AirportCanHaveZeroTourismMarkets()
    {
        // Create an airport without any tourism markets
        // Use a unique IATA code to avoid conflicts with other tests
        var uniqueIataCode = $"T{Guid.NewGuid():N}".Substring(0, 3).ToUpperInvariant();
        var airport = new Airport
        {
            Name = $"Test Airport {uniqueIataCode}",
            IataCode = uniqueIataCode,
            IcaoCode = $"K{uniqueIataCode}",
            City = "Test City",
            State = "Oregon",
            Country = "USA",
            Latitude = 45.5898,
            Longitude = -122.5951,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        DbContext.Airports.Add(airport);
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        var retrievedAirport = await DbContext.Airports
            .Include(a => a.AirportTourismMarkets)
            .FirstOrDefaultAsync(a => a.IataCode == uniqueIataCode).ConfigureAwait(false);

        Assert.NotNull(retrievedAirport);
        Assert.Empty(retrievedAirport.AirportTourismMarkets);
    }

    /// <summary>
    /// Tests that cascade delete works for CityTourismMarket when a city is deleted.
    /// </summary>
    [Fact]
    public async Task CityTourismMarketCascadeDeleteWhenCityDeleted()
    {
        var city = await DbContext.Cities
            .Include(c => c.CityTourismMarkets)
            .FirstOrDefaultAsync(c => c.Id == 2).ConfigureAwait(false); // Seattle

        Assert.NotNull(city);
        var relationshipCount = city.CityTourismMarkets.Count;
        Assert.True(relationshipCount > 0);

        DbContext.Cities.Remove(city);
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        var relationships = await DbContext.CityTourismMarkets
            .Where(ctm => ctm.CityId == 2)
            .ToListAsync().ConfigureAwait(false);

        Assert.Empty(relationships);
    }

    /// <summary>
    /// Tests that cascade delete works for AirportTourismMarket when an airport is deleted.
    /// </summary>
    [Fact]
    public async Task AirportTourismMarketCascadeDeleteWhenAirportDeleted()
    {
        var airport = await DbContext.Airports
            .Include(a => a.AirportTourismMarkets)
            .FirstOrDefaultAsync(a => a.Id == 2).ConfigureAwait(false); // SEA

        Assert.NotNull(airport);
        var relationshipCount = airport.AirportTourismMarkets.Count;
        Assert.True(relationshipCount > 0);

        DbContext.Airports.Remove(airport);
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        var relationships = await DbContext.AirportTourismMarkets
            .Where(atm => atm.AirportId == 2)
            .ToListAsync().ConfigureAwait(false);

        Assert.Empty(relationships);
    }

    /// <summary>
    /// Tests that the unique constraint prevents duplicate city-tourism market relationships.
    /// </summary>
    [Fact]
    public async Task CityTourismMarketUniqueConstraintPreventsDuplicates()
    {
        // Try to create a duplicate relationship
        var duplicate = new CityTourismMarket
        {
            CityId = 1,
            TourismMarketId = 1,
            CreatedAt = DateTimeOffset.UtcNow
        };

        DbContext.CityTourismMarkets.Add(duplicate);

        try
        {
            await DbContext.SaveChangesAsync().ConfigureAwait(false);
            Assert.True(false, "Expected DbUpdateException");
        }
        catch (DbUpdateException)
        {
            // Expected exception
        }
    }

    /// <summary>
    /// Tests that the unique constraint prevents duplicate airport-tourism market relationships.
    /// </summary>
    [Fact]
    public async Task AirportTourismMarketUniqueConstraintPreventsDuplicates()
    {
        // Try to create a duplicate relationship
        var duplicate = new AirportTourismMarket
        {
            AirportId = 1,
            TourismMarketId = 1,
            CreatedAt = DateTimeOffset.UtcNow
        };

        DbContext.AirportTourismMarkets.Add(duplicate);

        try
        {
            await DbContext.SaveChangesAsync().ConfigureAwait(false);
            Assert.True(false, "Expected DbUpdateException");
        }
        catch (DbUpdateException)
        {
            // Expected exception
        }
    }
}
