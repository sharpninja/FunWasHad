using System.Net;
using System.Net.Http.Json;
using FWH.MarketingApi.Data;
using FWH.MarketingApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FWH.MarketingApi.Tests;

/// <summary>
/// Unit tests for MarketingController.
/// Implements TR-TEST-001: Unit Tests for API controllers.
/// Implements TR-API-002: Marketing endpoints validation.
/// </summary>
public class MarketingControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public MarketingControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        SeedTestData();
    }

    private void SeedTestData()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MarketingDbContext>();

        // Clear existing data to avoid conflicts with other test classes
        db.Businesses.RemoveRange(db.Businesses);
        db.BusinessThemes.RemoveRange(db.BusinessThemes);
        db.Coupons.RemoveRange(db.Coupons);
        db.MenuItems.RemoveRange(db.MenuItems);
        db.NewsItems.RemoveRange(db.NewsItems);
        db.SaveChanges();

        var now = DateTimeOffset.UtcNow;

        // Add test business - MUST be subscribed for queries to work
        var business = new Business
        {
            Id = 1,
            Name = "Test Cafe",
            Address = "123 Main St",
            Latitude = 37.7749,
            Longitude = -122.4194,
            IsSubscribed = true, // Required for all queries
            CreatedAt = now
        };

        // Add theme - MUST be active for GetTheme to work
        var theme = new BusinessTheme
        {
            Id = 1,
            BusinessId = 1,
            Business = business, // Set navigation property
            ThemeName = "Test Theme",
            PrimaryColor = "#FF0000",
            IsActive = true, // Required for GetTheme query
            CreatedAt = now
        };
        business.Theme = theme; // Set navigation property
        db.BusinessThemes.Add(theme);

        // Add coupon - MUST be active and within valid date range
        var coupon = new Coupon
        {
            Id = 1,
            BusinessId = 1,
            Business = business, // Set navigation property
            Title = "Test Coupon",
            Description = "10% off",
            IsActive = true, // Required
            ValidFrom = now.AddDays(-1), // Must be in the past
            ValidUntil = now.AddDays(30), // Must be in the future
            CurrentRedemptions = 0,
            MaxRedemptions = null, // No limit
            CreatedAt = now
        };
        business.Coupons.Add(coupon); // Add to collection
        db.Coupons.Add(coupon);

        // Add menu item - MUST be available
        var menuItem = new MenuItem
        {
            Id = 1,
            BusinessId = 1,
            Business = business, // Set navigation property
            Name = "Test Item",
            Category = "Drinks",
            Price = 5.99m,
            IsAvailable = true, // Required
            SortOrder = 0,
            CreatedAt = now
        };
        business.MenuItems.Add(menuItem); // Add to collection
        db.MenuItems.Add(menuItem);

        // Add news item - MUST be published and PublishedAt in the past, no ExpiresAt
        var newsItem = new NewsItem
        {
            Id = 1,
            BusinessId = 1,
            Business = business, // Set navigation property
            Title = "Test News",
            Content = "Test content",
            IsPublished = true, // Required
            PublishedAt = now.AddDays(-1), // Must be in the past
            ExpiresAt = null, // No expiration
            IsFeatured = false,
            CreatedAt = now
        };
        business.NewsItems.Add(newsItem); // Add to collection
        db.NewsItems.Add(newsItem);

        db.Businesses.Add(business);

        db.SaveChanges();
    }

    /// <summary>
    /// Tests TR-API-002: GET /api/marketing/{businessId} - returns complete marketing data.
    /// </summary>
    [Fact]
    public async Task GetBusinessMarketing_ReturnsCompleteData()
    {
        var client = _factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });

        var response = await client.GetAsync("/api/marketing/1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<BusinessMarketingResponse>();
        Assert.NotNull(result);
        Assert.Equal(1, result!.BusinessId);
        Assert.Equal("Test Cafe", result.BusinessName);
        Assert.NotNull(result.Theme);
        Assert.Single(result.Coupons);
        Assert.Single(result.MenuItems);
        Assert.Single(result.NewsItems);
    }

    /// <summary>
    /// Tests TR-API-002: GET /api/marketing/{businessId} - returns 404 for non-existent business.
    /// </summary>
    [Fact]
    public async Task GetBusinessMarketing_NonExistentBusiness_ReturnsNotFound()
    {
        var client = _factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });

        var response = await client.GetAsync("/api/marketing/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// Tests TR-API-002: GET /api/marketing/{businessId}/theme - returns theme.
    /// </summary>
    [Fact]
    public async Task GetTheme_ReturnsTheme()
    {
        var client = _factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });

        var response = await client.GetAsync("/api/marketing/1/theme");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<BusinessTheme>();
        Assert.NotNull(result);
        Assert.Equal("Test Theme", result!.ThemeName);
    }

    /// <summary>
    /// Tests TR-API-002: GET /api/marketing/{businessId}/coupons - returns active coupons.
    /// </summary>
    [Fact]
    public async Task GetCoupons_ReturnsActiveCoupons()
    {
        var client = _factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });

        var response = await client.GetAsync("/api/marketing/1/coupons");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<List<Coupon>>();
        Assert.NotNull(result);
        Assert.Single(result!);
        Assert.Equal("Test Coupon", result[0].Title);
    }

    /// <summary>
    /// Tests TR-API-002: GET /api/marketing/{businessId}/menu - returns menu items.
    /// </summary>
    [Fact]
    public async Task GetMenu_ReturnsMenuItems()
    {
        var client = _factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });

        var response = await client.GetAsync("/api/marketing/1/menu");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<List<MenuItem>>();
        Assert.NotNull(result);
        Assert.Single(result!);
        Assert.Equal("Test Item", result[0].Name);
    }

    /// <summary>
    /// Tests TR-API-002: GET /api/marketing/{businessId}/menu/categories - returns categories.
    /// </summary>
    [Fact]
    public async Task GetMenuCategories_ReturnsCategories()
    {
        var client = _factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });

        var response = await client.GetAsync("/api/marketing/1/menu/categories");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<List<string>>();
        Assert.NotNull(result);
        Assert.Single(result!);
        Assert.Equal("Drinks", result[0]);
    }

    /// <summary>
    /// Tests TR-API-002: GET /api/marketing/{businessId}/news - returns news items.
    /// </summary>
    [Fact]
    public async Task GetNews_ReturnsNewsItems()
    {
        var client = _factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });

        var response = await client.GetAsync("/api/marketing/1/news");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<List<NewsItem>>();
        Assert.NotNull(result);
        Assert.Single(result!);
        Assert.Equal("Test News", result[0].Title);
    }

    /// <summary>
    /// Tests TR-API-002: GET /api/marketing/nearby - returns nearby businesses.
    /// Implements TR-SEC-001: Data validation for coordinates.
    /// </summary>
    [Fact]
    public async Task GetNearbyBusinesses_ReturnsNearbyBusinesses()
    {
        var client = _factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });

        var response = await client.GetAsync("/api/marketing/nearby?latitude=37.7749&longitude=-122.4194&radiusMeters=1000");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<List<Business>>();
        Assert.NotNull(result);
        Assert.Single(result!);
        Assert.Equal("Test Cafe", result[0].Name);
    }

    /// <summary>
    /// Tests TR-API-002: GET /api/marketing/nearby - invalid latitude returns bad request.
    /// Implements TR-SEC-001: Data validation.
    /// </summary>
    [Fact]
    public async Task GetNearbyBusinesses_InvalidLatitude_ReturnsBadRequest()
    {
        var client = _factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });

        var response = await client.GetAsync("/api/marketing/nearby?latitude=999&longitude=-122.4194");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Tests TR-API-002: GET /api/marketing/nearby - invalid longitude returns bad request.
    /// Implements TR-SEC-001: Data validation.
    /// </summary>
    [Fact]
    public async Task GetNearbyBusinesses_InvalidLongitude_ReturnsBadRequest()
    {
        var client = _factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });

        var response = await client.GetAsync("/api/marketing/nearby?latitude=37.7749&longitude=999");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
