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
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The GET /api/marketing/{businessId} endpoint's ability to return complete marketing data including business info, theme, coupons, menu items, and news items.</para>
    /// <para><strong>Data involved:</strong> Business ID 1, which has been seeded with a complete marketing profile: business "Test Cafe" at coordinates (37.7749, -122.4194), an active theme, one active coupon, one available menu item, and one published news item. The business is marked as subscribed (IsSubscribed = true), which is required for the query to return results.</para>
    /// <para><strong>Why the data matters:</strong> The endpoint aggregates data from multiple database tables (Businesses, BusinessThemes, Coupons, MenuItems, NewsItems). Testing with a complete dataset ensures all relationships and filtering logic work correctly. The subscribed status is critical because the endpoint filters out non-subscribed businesses.</para>
    /// <para><strong>Expected outcome:</strong> HTTP 200 OK response with BusinessMarketingResponse containing businessId=1, businessName="Test Cafe", a non-null theme, and exactly one item in each collection (Coupons, MenuItems, NewsItems).</para>
    /// <para><strong>Reason for expectation:</strong> The seeded test data includes exactly one of each entity type, and all entities are in the correct state (active, available, published) to be returned by the query. The endpoint should aggregate all related data and return it in a single response, which is the expected behavior for a marketing API that provides complete business information.</para>
    /// </remarks>
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
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The GET /api/marketing/{businessId} endpoint's error handling when a non-existent business ID is requested.</para>
    /// <para><strong>Data involved:</strong> Business ID 999, which does not exist in the test database. The test database only contains business ID 1 from the SeedTestData method.</para>
    /// <para><strong>Why the data matters:</strong> Error handling is critical for API robustness. Testing with a non-existent ID ensures the endpoint correctly handles invalid requests and returns appropriate HTTP status codes rather than throwing exceptions or returning null data.</para>
    /// <para><strong>Expected outcome:</strong> HTTP 404 Not Found status code.</para>
    /// <para><strong>Reason for expectation:</strong> REST API best practices dictate that requests for non-existent resources should return 404 Not Found. This allows clients to distinguish between "resource doesn't exist" (404) and "server error" (500), enabling proper error handling in client applications.</para>
    /// </remarks>
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
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The GET /api/marketing/{businessId}/theme endpoint's ability to return the active theme for a business.</para>
    /// <para><strong>Data involved:</strong> Business ID 1, which has an active theme (IsActive = true) with themeName "Test Theme" and primaryColor "#FF0000". The theme is linked to the business via BusinessId foreign key.</para>
    /// <para><strong>Why the data matters:</strong> Theme retrieval is used for UI customization - the mobile app needs to display business-specific colors and branding. The IsActive flag ensures only active themes are returned, allowing businesses to have multiple themes but only one active at a time.</para>
    /// <para><strong>Expected outcome:</strong> HTTP 200 OK response with BusinessTheme object containing the theme data (Id=1, BusinessId=1, ThemeName="Test Theme", PrimaryColor="#FF0000", IsActive=true).</para>
    /// <para><strong>Reason for expectation:</strong> The seeded test data includes an active theme for business 1. The endpoint should query the BusinessThemes table, filter by BusinessId and IsActive=true, and return the matching theme. This is the expected behavior for theme retrieval in a multi-tenant system where businesses can customize their appearance.</para>
    /// </remarks>
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
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The GET /api/marketing/{businessId}/coupons endpoint's ability to return only active coupons that are within their valid date range.</para>
    /// <para><strong>Data involved:</strong> Business ID 1, which has one active coupon (IsActive = true) with ValidFrom in the past and ValidUntil in the future. The coupon has title "Test Coupon" and description "10% off".</para>
    /// <para><strong>Why the data matters:</strong> Coupon filtering is critical for user experience - only valid, active coupons should be displayed. The endpoint must filter by IsActive flag and date range (ValidFrom ≤ now ≤ ValidUntil) to ensure users only see redeemable coupons.</para>
    /// <para><strong>Expected outcome:</strong> HTTP 200 OK response with a list containing exactly one Coupon object with Title="Test Coupon".</para>
    /// <para><strong>Reason for expectation:</strong> The seeded test data includes exactly one active coupon within its validity period. The endpoint should filter out inactive coupons and expired/future coupons, returning only the valid active coupon. This ensures users don't see coupons they can't use.</para>
    /// </remarks>
    [Fact]
    public async Task GetCoupons_ReturnsActiveCoupons()
    {
        var client = _factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });

        var response = await client.GetAsync("/api/marketing/1/coupons");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<Coupon>>();
        Assert.NotNull(result);
        Assert.Single(result!.Items);
        Assert.Equal("Test Coupon", result.Items[0].Title);
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(1, result.Page);
    }

    /// <summary>
    /// Tests TR-API-002: GET /api/marketing/{businessId}/menu - returns menu items.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The GET /api/marketing/{businessId}/menu endpoint's ability to return available menu items for a business.</para>
    /// <para><strong>Data involved:</strong> Business ID 1, which has one available menu item (IsAvailable = true) with name "Test Item", category "Drinks", and price 5.99. The menu item is linked to the business via BusinessId foreign key.</para>
    /// <para><strong>Why the data matters:</strong> Menu retrieval is essential for businesses like restaurants and cafes - customers need to see what items are available. The IsAvailable flag ensures only available items are returned, allowing businesses to temporarily disable items that are out of stock.</para>
    /// <para><strong>Expected outcome:</strong> HTTP 200 OK response with a list containing exactly one MenuItem object with Name="Test Item".</para>
    /// <para><strong>Reason for expectation:</strong> The seeded test data includes exactly one available menu item for business 1. The endpoint should query the MenuItems table, filter by BusinessId and IsAvailable=true, and return the matching items. This ensures customers only see items they can actually order.</para>
    /// </remarks>
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
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The GET /api/marketing/{businessId}/menu/categories endpoint's ability to return distinct menu item categories for a business.</para>
    /// <para><strong>Data involved:</strong> Business ID 1, which has one menu item with category "Drinks". The endpoint should extract and return distinct categories from all available menu items for the business.</para>
    /// <para><strong>Why the data matters:</strong> Menu categories enable customers to filter and browse menu items by type (e.g., "Appetizers", "Main Courses", "Desserts", "Drinks"). This improves user experience by organizing menu items into logical groups.</para>
    /// <para><strong>Expected outcome:</strong> HTTP 200 OK response with a list containing exactly one string "Drinks".</para>
    /// <para><strong>Reason for expectation:</strong> The seeded test data includes exactly one menu item with category "Drinks". The endpoint should query all available menu items for the business, extract distinct category values, and return them as a list. This allows the UI to display category filters or organize items by category.</para>
    /// </remarks>
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
        var result = await response.Content.ReadFromJsonAsync<PagedResult<NewsItem>>();
        Assert.NotNull(result);
        Assert.Single(result!.Items);
        Assert.Equal("Test News", result.Items[0].Title);
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(1, result.Page);
    }

    /// <summary>
    /// Tests TR-API-002: GET /api/marketing/nearby - returns nearby businesses.
    /// Implements TR-SEC-001: Data validation for coordinates.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The GET /api/marketing/nearby endpoint's ability to find businesses within a specified radius of given coordinates.</para>
    /// <para><strong>Data involved:</strong> Query parameters: latitude=37.7749, longitude=-122.4194 (San Francisco coordinates), radiusMeters=1000. The test database contains "Test Cafe" at coordinates (37.7749, -122.4194), which is exactly at the query location, so it should be within any radius.</para>
    /// <para><strong>Why the data matters:</strong> Location-based business discovery is a core feature. The endpoint must correctly calculate distances using the Haversine formula and filter businesses within the specified radius. The 1000-meter radius is a common search distance for nearby businesses.</para>
    /// <para><strong>Expected outcome:</strong> HTTP 200 OK response with a list containing exactly one Business object with Name="Test Cafe".</para>
    /// <para><strong>Reason for expectation:</strong> The test business is located at exactly the same coordinates as the query point, so the distance is 0 meters, which is well within the 1000-meter radius. The endpoint should use spatial distance calculation to find all businesses within the radius and return them as a list.</para>
    /// </remarks>
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
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The GET /api/marketing/nearby endpoint's input validation for invalid latitude values.</para>
    /// <para><strong>Data involved:</strong> Query parameter latitude=999, which is outside the valid range of -90 to 90 degrees. Longitude=-122.4194 is valid. Invalid latitude values can cause calculation errors or security issues if not validated.</para>
    /// <para><strong>Why the data matters:</strong> Input validation is critical for API security and data integrity. Invalid coordinates can cause exceptions in distance calculations, database queries, or lead to unexpected behavior. The endpoint must validate coordinate ranges before processing.</para>
    /// <para><strong>Expected outcome:</strong> HTTP 400 Bad Request status code.</para>
    /// <para><strong>Reason for expectation:</strong> REST API best practices require validation of input parameters. Latitude must be between -90 and 90 degrees (valid range for Earth coordinates). Returning 400 Bad Request allows clients to correct invalid input, distinguishing it from server errors (500) or not found (404).</para>
    /// </remarks>
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
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The GET /api/marketing/nearby endpoint's input validation for invalid longitude values.</para>
    /// <para><strong>Data involved:</strong> Query parameter longitude=999, which is outside the valid range of -180 to 180 degrees. Latitude=37.7749 is valid. Invalid longitude values can cause calculation errors or security issues if not validated.</para>
    /// <para><strong>Why the data matters:</strong> Input validation is critical for API security and data integrity. Invalid coordinates can cause exceptions in distance calculations, database queries, or lead to unexpected behavior. The endpoint must validate coordinate ranges before processing.</para>
    /// <para><strong>Expected outcome:</strong> HTTP 400 Bad Request status code.</para>
    /// <para><strong>Reason for expectation:</strong> REST API best practices require validation of input parameters. Longitude must be between -180 and 180 degrees (valid range for Earth coordinates). Returning 400 Bad Request allows clients to correct invalid input, distinguishing it from server errors (500) or not found (404).</para>
    /// </remarks>
    [Fact]
    public async Task GetNearbyBusinesses_InvalidLongitude_ReturnsBadRequest()
    {
        var client = _factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });

        var response = await client.GetAsync("/api/marketing/nearby?latitude=37.7749&longitude=999");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
