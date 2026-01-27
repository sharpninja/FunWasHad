using FWH.MarketingApi.Controllers;
using FWH.MarketingApi.Models;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace FWH.MarketingApi.Tests;

/// <summary>
/// Unit tests for MarketingController.
/// Implements TR-TEST-001: Unit Tests for API controllers.
/// Implements TR-API-002: Marketing endpoints validation.
/// </summary>
public class MarketingControllerTests : ControllerTestBase
{
    private MarketingController CreateController()
    {
        return new MarketingController(DbContext, CreateLogger<MarketingController>());
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
    public async Task GetBusinessMarketingReturnsCompleteData()
    {
        // Arrange
        SeedCompleteTestBusiness();
        var controller = CreateController();

        // Act
        var result = await controller.GetBusinessMarketing(1);

        // Assert
        var okResult = Assert.IsType<ActionResult<BusinessMarketingResponse>>(result);
        var actionResult = okResult.Result as OkObjectResult;
        Assert.NotNull(actionResult);
        var response = Assert.IsType<BusinessMarketingResponse>(actionResult.Value);
        Assert.Equal(1, response.BusinessId);
        Assert.Equal("Test Cafe", response.BusinessName);
        Assert.NotNull(response.Theme);
        Assert.Single(response.Coupons);
        Assert.Single(response.MenuItems);
        Assert.Single(response.NewsItems);
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
    public async Task GetBusinessMarketingNonExistentBusinessReturnsNotFound()
    {
        // Arrange
        var controller = CreateController();

        // Act
        var result = await controller.GetBusinessMarketing(999);

        // Assert
        var actionResult = Assert.IsType<ActionResult<BusinessMarketingResponse>>(result);
        Assert.IsType<NotFoundResult>(actionResult.Result);
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
    public async Task GetThemeReturnsTheme()
    {
        // Arrange
        SeedTestBusinessWithTheme();
        var controller = CreateController();

        // Act
        var result = await controller.GetTheme(1);

        // Assert
        var okResult = Assert.IsType<ActionResult<BusinessTheme>>(result);
        var actionResult = okResult.Result as OkObjectResult;
        Assert.NotNull(actionResult);
        var theme = Assert.IsType<BusinessTheme>(actionResult.Value);
        Assert.Equal("Test Theme", theme.ThemeName);
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
    public async Task GetCouponsReturnsActiveCoupons()
    {
        // Arrange
        SeedTestBusinessWithCoupons();
        var controller = CreateController();

        // Act
        var result = await controller.GetCoupons(1, 1, 10);

        // Assert
        var okResult = Assert.IsType<ActionResult<PagedResult<Coupon>>>(result);
        var actionResult = okResult.Result as OkObjectResult;
        Assert.NotNull(actionResult);
        var pagedResult = Assert.IsType<PagedResult<Coupon>>(actionResult.Value);
        Assert.Single(pagedResult.Items);
        Assert.Equal("Test Coupon", pagedResult.Items[0].Title);
        Assert.Equal(1, pagedResult.TotalCount);
        Assert.Equal(1, pagedResult.Page);
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
    public async Task GetMenuReturnsMenuItems()
    {
        // Arrange
        SeedTestBusinessWithMenuItems();
        var controller = CreateController();

        // Act
        var result = await controller.GetMenu(1);

        // Assert
        var okResult = Assert.IsType<ActionResult<List<MenuItem>>>(result);
        var actionResult = okResult.Result as OkObjectResult;
        Assert.NotNull(actionResult);
        var menuItems = Assert.IsType<List<MenuItem>>(actionResult.Value);
        Assert.Single(menuItems);
        Assert.Equal("Test Item", menuItems[0].Name);
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
    public async Task GetMenuCategoriesReturnsCategories()
    {
        // Arrange
        SeedTestBusinessWithMenuItems();
        var controller = CreateController();

        // Act
        var result = await controller.GetMenuCategories(1);

        // Assert
        var okResult = Assert.IsType<ActionResult<List<string>>>(result);
        var actionResult = okResult.Result as OkObjectResult;
        Assert.NotNull(actionResult);
        var categories = Assert.IsType<List<string>>(actionResult.Value);
        Assert.Single(categories);
        Assert.Equal("Drinks", categories[0]);
    }

    /// <summary>
    /// Tests TR-API-002: GET /api/marketing/{businessId}/news - returns news items.
    /// </summary>
    [Fact]
    public async Task GetNewsReturnsNewsItems()
    {
        // Arrange
        SeedTestBusinessWithNewsItems();
        var controller = CreateController();

        // Act
        var result = await controller.GetNews(1, 1, 10).ConfigureAwait(true);

        // Assert
        var okResult = Assert.IsType<ActionResult<PagedResult<NewsItem>>>(result);
        var actionResult = okResult.Result as OkObjectResult;
        Assert.NotNull(actionResult);
        var pagedResult = Assert.IsType<PagedResult<NewsItem>>(actionResult.Value);
        Assert.Single(pagedResult.Items);
        Assert.Equal("Test News", pagedResult.Items[0].Title);
        Assert.Equal(1, pagedResult.TotalCount);
        Assert.Equal(1, pagedResult.Page);
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
    public async Task GetNearbyBusinessesReturnsNearbyBusinesses()
    {
        // Arrange
        SeedCompleteTestBusiness();
        var controller = CreateController();

        // Act
        var result = await controller.GetNearbyBusinesses(37.7749, -122.4194, 1000).ConfigureAwait(true);

        // Assert
        var okResult = Assert.IsType<ActionResult<List<Business>>>(result);
        var actionResult = okResult.Result as OkObjectResult;
        Assert.NotNull(actionResult);
        var businesses = Assert.IsType<List<Business>>(actionResult.Value);
        Assert.Single(businesses);
        Assert.Equal("Test Cafe", businesses[0].Name);
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
    public async Task GetNearbyBusinessesInvalidLatitudeReturnsBadRequest()
    {
        // Arrange
        var controller = CreateController();

        // Act
        var result = await controller.GetNearbyBusinesses(999, -122.4194, 1000);

        // Assert
        var actionResult = Assert.IsType<ActionResult<List<Business>>>(result);
        Assert.IsType<BadRequestObjectResult>(actionResult.Result);
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
    public async Task GetNearbyBusinessesInvalidLongitudeReturnsBadRequest()
    {
        // Arrange
        var controller = CreateController();

        // Act
        var result = await controller.GetNearbyBusinesses(37.7749, 999, 1000);

        // Assert
        var actionResult = Assert.IsType<ActionResult<List<Business>>>(result);
        Assert.IsType<BadRequestObjectResult>(actionResult.Result);
    }
}
