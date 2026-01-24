using System.Net;
using System.Net.Http.Json;
using FWH.MarketingApi.Data;
using FWH.MarketingApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FWH.MarketingApi.Tests;

/// <summary>
/// Tests for city marketing information retrieval endpoints.
/// Implements TR-API-002: Marketing endpoints validation.
/// </summary>
public class CityMarketingTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public CityMarketingTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        SeedTestData();
    }

    private void SeedTestData()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MarketingDbContext>();

        // Clear existing data
        db.CityTourismMarkets.RemoveRange(db.CityTourismMarkets);
        db.CityThemes.RemoveRange(db.CityThemes);
        db.Cities.RemoveRange(db.Cities);
        db.SaveChanges();

        // Reset sequences to avoid ID conflicts
        try
        {
            var maxCityId = db.Cities.Any() ? db.Cities.Max(c => c.Id) : 0;
            var maxCityThemeId = db.CityThemes.Any() ? db.CityThemes.Max(c => c.Id) : 0;
            var maxCityTourismMarketId = db.CityTourismMarkets.Any() ? db.CityTourismMarkets.Max(c => c.Id) : 0;

            db.Database.ExecuteSqlRaw("SELECT setval('cities_id_seq', {0}, true)", maxCityId);
            db.Database.ExecuteSqlRaw("SELECT setval('city_themes_id_seq', {0}, true)", maxCityThemeId);
            db.Database.ExecuteSqlRaw("SELECT setval('city_tourism_markets_id_seq', {0}, true)", maxCityTourismMarketId);
        }
        catch
        {
            // Sequences might not exist yet or tables are empty, ignore
        }

        var now = DateTimeOffset.UtcNow;

        // Create city with theme
        var city = new City
        {
            Id = 1,
            Name = "San Francisco",
            State = "California",
            Country = "USA",
            Latitude = 37.7749,
            Longitude = -122.4194,
            Description = "The City by the Bay",
            Website = "https://sf.gov",
            IsActive = true,
            CreatedAt = now
        };

        var theme = new CityTheme
        {
            Id = 1,
            CityId = 1,
            City = city,
            ThemeName = "SF Theme",
            PrimaryColor = "#003366",
            SecondaryColor = "#FF6600",
            LogoUrl = "https://sf.gov/logo.png",
            BackgroundImageUrl = "https://sf.gov/bg.jpg",
            IsActive = true,
            CreatedAt = now
        };

        city.Theme = theme;

        db.Cities.Add(city);
        db.CityThemes.Add(theme);

        // Create another city without theme
        var city2 = new City
        {
            Id = 2,
            Name = "Seattle",
            State = "Washington",
            Country = "USA",
            Latitude = 47.6062,
            Longitude = -122.3321,
            Description = "The Emerald City",
            IsActive = true,
            CreatedAt = now
        };

        db.Cities.Add(city2);

        // Create city with inactive theme (should not be returned)
        var city3 = new City
        {
            Id = 3,
            Name = "Portland",
            State = "Oregon",
            Country = "USA",
            Latitude = 45.5152,
            Longitude = -122.6784,
            IsActive = true,
            CreatedAt = now
        };

        var inactiveTheme = new CityTheme
        {
            Id = 2,
            CityId = 3,
            City = city3,
            ThemeName = "Portland Theme",
            PrimaryColor = "#00AA00",
            IsActive = false, // Inactive
            CreatedAt = now
        };

        city3.Theme = inactiveTheme;

        db.Cities.Add(city3);
        db.CityThemes.Add(inactiveTheme);

        db.SaveChanges();
    }

    /// <summary>
    /// Tests GET /api/marketing/city - returns city marketing data with theme.
    /// </summary>
    [Fact]
    public async Task GetCityMarketing_ReturnsCityWithTheme()
    {
        var client = _factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });

        var response = await client.GetAsync("/api/marketing/city?cityName=San Francisco&state=California&country=USA");

        if (response.StatusCode != HttpStatusCode.OK)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"API returned {response.StatusCode}: {errorContent}");
        }

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<CityMarketingResponse>();
        Assert.NotNull(result);
        Assert.Equal(1, result!.CityId);
        Assert.Equal("San Francisco", result.CityName);
        Assert.Equal("California", result.State);
        Assert.Equal("USA", result.Country);
        Assert.Equal("The City by the Bay", result.Description);
        Assert.Equal("https://sf.gov", result.Website);
        Assert.NotNull(result.Theme);
        Assert.Equal("SF Theme", result.Theme!.ThemeName);
        Assert.Equal("#003366", result.Theme.PrimaryColor);
    }

    /// <summary>
    /// Tests GET /api/marketing/city - returns city without theme when theme is inactive.
    /// </summary>
    [Fact]
    public async Task GetCityMarketing_ReturnsCityWithoutInactiveTheme()
    {
        var client = _factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });

        var response = await client.GetAsync("/api/marketing/city?cityName=Portland&state=Oregon&country=USA");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<CityMarketingResponse>();
        Assert.NotNull(result);
        Assert.Equal(3, result!.CityId);
        Assert.Equal("Portland", result.CityName);
        // Theme should be null because it's inactive
        Assert.Null(result.Theme);
    }

    /// <summary>
    /// Tests GET /api/marketing/city - returns city without theme when no theme exists.
    /// </summary>
    [Fact]
    public async Task GetCityMarketing_ReturnsCityWithoutTheme()
    {
        var client = _factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });

        var response = await client.GetAsync("/api/marketing/city?cityName=Seattle&state=Washington&country=USA");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<CityMarketingResponse>();
        Assert.NotNull(result);
        Assert.Equal(2, result!.CityId);
        Assert.Equal("Seattle", result.CityName);
        Assert.Null(result.Theme);
    }

    /// <summary>
    /// Tests GET /api/marketing/city - returns 404 for non-existent city.
    /// </summary>
    [Fact]
    public async Task GetCityMarketing_NonExistentCity_ReturnsNotFound()
    {
        var client = _factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });

        var response = await client.GetAsync("/api/marketing/city?cityName=NonExistent&state=Nowhere&country=USA");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// Tests GET /api/marketing/city - returns 400 when city name is missing.
    /// </summary>
    [Fact]
    public async Task GetCityMarketing_MissingCityName_ReturnsBadRequest()
    {
        var client = _factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });

        var response = await client.GetAsync("/api/marketing/city?state=California&country=USA");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Tests GET /api/marketing/city - case-insensitive city name matching.
    /// </summary>
    [Fact]
    public async Task GetCityMarketing_CaseInsensitiveMatching()
    {
        var client = _factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });

        var response = await client.GetAsync("/api/marketing/city?cityName=san francisco&state=california&country=usa");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<CityMarketingResponse>();
        Assert.NotNull(result);
        Assert.Equal("San Francisco", result!.CityName);
    }

    /// <summary>
    /// Tests GET /api/marketing/city/{cityId}/theme - returns active city theme.
    /// </summary>
    [Fact]
    public async Task GetCityTheme_ReturnsActiveTheme()
    {
        var client = _factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });

        var response = await client.GetAsync("/api/marketing/city/1/theme");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<CityTheme>();
        Assert.NotNull(result);
        Assert.Equal(1, result!.CityId);
        Assert.Equal("SF Theme", result.ThemeName);
        Assert.Equal("#003366", result.PrimaryColor);
        Assert.True(result.IsActive);
    }

    /// <summary>
    /// Tests GET /api/marketing/city/{cityId}/theme - returns 404 for city without active theme.
    /// </summary>
    [Fact]
    public async Task GetCityTheme_NoActiveTheme_ReturnsNotFound()
    {
        var client = _factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });

        var response = await client.GetAsync("/api/marketing/city/2/theme");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// Tests GET /api/marketing/city/{cityId}/theme - returns 404 for inactive theme.
    /// </summary>
    [Fact]
    public async Task GetCityTheme_InactiveTheme_ReturnsNotFound()
    {
        var client = _factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });

        var response = await client.GetAsync("/api/marketing/city/3/theme");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// Tests GET /api/marketing/city/{cityId}/theme - returns 404 for non-existent city.
    /// </summary>
    [Fact]
    public async Task GetCityTheme_NonExistentCity_ReturnsNotFound()
    {
        var client = _factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });

        var response = await client.GetAsync("/api/marketing/city/999/theme");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// Tests that cities can be retrieved with their tourism markets via navigation properties.
    /// </summary>
    [Fact]
    public async Task City_CanRetrieveTourismMarkets()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MarketingDbContext>();

        // Create tourism market
        var market = new TourismMarket
        {
            Name = "Test Market",
            Description = "Test Description",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.TourismMarkets.Add(market);
        await db.SaveChangesAsync();

        // Create city and link to market
        var city = await db.Cities.FirstOrDefaultAsync(c => c.Id == 1);
        Assert.NotNull(city);

        var relationship = new CityTourismMarket
        {
            CityId = city.Id,
            TourismMarketId = market.Id,
            City = city,
            TourismMarket = market,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.CityTourismMarkets.Add(relationship);
        await db.SaveChangesAsync();

        // Retrieve city with markets
        var cityWithMarkets = await db.Cities
            .Include(c => c.CityTourismMarkets)
                .ThenInclude(ctm => ctm.TourismMarket)
            .FirstOrDefaultAsync(c => c.Id == city.Id);

        Assert.NotNull(cityWithMarkets);
        Assert.NotEmpty(cityWithMarkets.CityTourismMarkets);
        Assert.Contains(cityWithMarkets.CityTourismMarkets, ctm => ctm.TourismMarket.Name == "Test Market");
    }
}
