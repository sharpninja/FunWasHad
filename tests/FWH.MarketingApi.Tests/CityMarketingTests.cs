using FWH.MarketingApi.Controllers;
using FWH.MarketingApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FWH.MarketingApi.Tests;

/// <summary>
/// Tests for city marketing information retrieval endpoints.
/// Implements TR-API-002: Marketing endpoints validation.
/// </summary>
public class CityMarketingTests : ControllerTestBase
{
    private MarketingController CreateController()
    {
        return new MarketingController(DbContext, CreateLogger<MarketingController>());
    }

    /// <summary>
    /// Tests GET /api/marketing/city - returns city marketing data with theme.
    /// </summary>
    [Fact]
    public async Task GetCityMarketingReturnsCityWithTheme()
    {
        // Arrange
        SeedTestCityWithTheme();
        var controller = CreateController();

        // Act
        var result = await controller.GetCityMarketing("San Francisco", "California", "USA").ConfigureAwait(true);

        // Assert
        var okResult = Assert.IsType<ActionResult<CityMarketingResponse>>(result);
        var actionResult = okResult.Result as OkObjectResult;
        Assert.NotNull(actionResult);
        var response = Assert.IsType<CityMarketingResponse>(actionResult.Value);
        Assert.Equal(1, response.CityId);
        Assert.Equal("San Francisco", response.CityName);
        Assert.Equal("California", response.State);
        Assert.Equal("USA", response.Country);
        Assert.Equal("The City by the Bay", response.Description);
        Assert.Equal("https://sf.gov", response.Website);
        Assert.NotNull(response.Theme);
        Assert.Equal("SF Theme", response.Theme!.ThemeName);
        Assert.Equal("#003366", response.Theme.PrimaryColor);
    }

    /// <summary>
    /// Tests GET /api/marketing/city - returns city without theme when theme is inactive.
    /// </summary>
    [Fact]
    public async Task GetCityMarketingReturnsCityWithoutInactiveTheme()
    {
        // Arrange
        SeedTestCityWithInactiveTheme();
        var controller = CreateController();

        // Act
        var result = await controller.GetCityMarketing("Portland", "Oregon", "USA").ConfigureAwait(true);

        // Assert
        var okResult = Assert.IsType<ActionResult<CityMarketingResponse>>(result);
        var actionResult = okResult.Result as OkObjectResult;
        Assert.NotNull(actionResult);
        var response = Assert.IsType<CityMarketingResponse>(actionResult.Value);
        Assert.Equal(3, response.CityId);
        Assert.Equal("Portland", response.CityName);
        // Theme should be null because it's inactive
        Assert.Null(response.Theme);
    }

    /// <summary>
    /// Tests GET /api/marketing/city - returns city without theme when no theme exists.
    /// </summary>
    [Fact]
    public async Task GetCityMarketing_ReturnsCityWithoutTheme()
    {
        // Arrange
        SeedTestCityWithoutTheme();
        var controller = CreateController();

        // Act
        var result = await controller.GetCityMarketing("Seattle", "Washington", "USA");

        // Assert
        var okResult = Assert.IsType<ActionResult<CityMarketingResponse>>(result);
        var actionResult = okResult.Result as OkObjectResult;
        Assert.NotNull(actionResult);
        var response = Assert.IsType<CityMarketingResponse>(actionResult.Value);
        Assert.Equal(2, response.CityId);
        Assert.Equal("Seattle", response.CityName);
        Assert.Null(response.Theme);
    }

    /// <summary>
    /// Tests GET /api/marketing/city - returns 404 for non-existent city.
    /// </summary>
    [Fact]
    public async Task GetCityMarketingNonExistentCityReturnsNotFound()
    {
        // Arrange
        var controller = CreateController();

        // Act
        var result = await controller.GetCityMarketing("NonExistent", "Nowhere", "USA");

        // Assert
        var actionResult = Assert.IsType<ActionResult<CityMarketingResponse>>(result);
        Assert.IsType<NotFoundResult>(actionResult.Result);
    }

    /// <summary>
    /// Tests GET /api/marketing/city - returns 400 when city name is missing.
    /// </summary>
    [Fact]
    public async Task GetCityMarketingMissingCityNameReturnsBadRequest()
    {
        // Arrange
        var controller = CreateController();

        // Act
        var result = await controller.GetCityMarketing("", "California", "USA").ConfigureAwait(true);

        // Assert
        var actionResult = Assert.IsType<ActionResult<CityMarketingResponse>>(result);
        Assert.IsType<BadRequestObjectResult>(actionResult.Result);
    }

    /// <summary>
    /// Tests GET /api/marketing/city - case-insensitive city name matching.
    /// </summary>
    [Fact]
    public async Task GetCityMarketing_CaseInsensitiveMatching()
    {
        // Arrange
        SeedTestCityWithTheme();
        var controller = CreateController();

        // Act
        var result = await controller.GetCityMarketing("san francisco", "california", "usa").ConfigureAwait(true);

        // Assert
        var okResult = Assert.IsType<ActionResult<CityMarketingResponse>>(result);
        var actionResult = okResult.Result as OkObjectResult;
        Assert.NotNull(actionResult);
        var response = Assert.IsType<CityMarketingResponse>(actionResult.Value);
        Assert.Equal("San Francisco", response.CityName);
    }

    /// <summary>
    /// Tests GET /api/marketing/city/{cityId}/theme - returns active city theme.
    /// </summary>
    [Fact]
    public async Task GetCityTheme_ReturnsActiveTheme()
    {
        // Arrange
        SeedTestCityWithTheme();
        var controller = CreateController();

        // Act
        var result = await controller.GetCityTheme(1).ConfigureAwait(true);

        // Assert
        var okResult = Assert.IsType<ActionResult<CityTheme>>(result);
        var actionResult = okResult.Result as OkObjectResult;
        Assert.NotNull(actionResult);
        var theme = Assert.IsType<CityTheme>(actionResult.Value);
        Assert.Equal(1, theme.CityId);
        Assert.Equal("SF Theme", theme.ThemeName);
        Assert.Equal("#003366", theme.PrimaryColor);
        Assert.True(theme.IsActive);
    }

    /// <summary>
    /// Tests GET /api/marketing/city/{cityId}/theme - returns 404 for city without active theme.
    /// </summary>
    [Fact]
    public async Task GetCityThemeNoActiveThemeReturnsNotFound()
    {
        // Arrange
        SeedTestCityWithoutTheme();
        var controller = CreateController();

        // Act
        var result = await controller.GetCityTheme(2).ConfigureAwait(true);

        // Assert
        var actionResult = Assert.IsType<ActionResult<CityTheme>>(result);
        Assert.IsType<NotFoundResult>(actionResult.Result);
    }

    /// <summary>
    /// Tests GET /api/marketing/city/{cityId}/theme - returns 404 for inactive theme.
    /// </summary>
    [Fact]
    public async Task GetCityThemeInactiveThemeReturnsNotFound()
    {
        // Arrange
        SeedTestCityWithInactiveTheme();
        var controller = CreateController();

        // Act
        var result = await controller.GetCityTheme(3);

        // Assert
        var actionResult = Assert.IsType<ActionResult<CityTheme>>(result);
        Assert.IsType<NotFoundResult>(actionResult.Result);
    }

    /// <summary>
    /// Tests GET /api/marketing/city/{cityId}/theme - returns 404 for non-existent city.
    /// </summary>
    [Fact]
    public async Task GetCityTheme_NonExistentCity_ReturnsNotFound()
    {
        // Arrange
        var controller = CreateController();

        // Act
        var result = await controller.GetCityTheme(999);

        // Assert
        var actionResult = Assert.IsType<ActionResult<CityTheme>>(result);
        Assert.IsType<NotFoundResult>(actionResult.Result);
    }

    /// <summary>
    /// Tests that cities can be retrieved with their tourism markets via navigation properties.
    /// </summary>
    [Fact]
    public async Task CityCanRetrieveTourismMarkets()
    {
        // Arrange
        SeedTestCityWithTheme();
        
        // Create tourism market
        var market = new TourismMarket
        {
            Name = "Test Market",
            Description = "Test Description",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
        DbContext.TourismMarkets.Add(market);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Create city and link to market
        var city = await DbContext.Cities.FirstOrDefaultAsync(c => c.Id == 1, TestContext.Current.CancellationToken);
        Assert.NotNull(city);

        var relationship = new CityTourismMarket
        {
            CityId = city.Id,
            TourismMarketId = market.Id,
            City = city,
            TourismMarket = market,
            CreatedAt = DateTimeOffset.UtcNow
        };
        DbContext.CityTourismMarkets.Add(relationship);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act - Retrieve city with markets
        var cityWithMarkets = await DbContext.Cities
            .Include(c => c.CityTourismMarkets)
                .ThenInclude(ctm => ctm.TourismMarket)
            .FirstOrDefaultAsync(c => c.Id == city.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(cityWithMarkets);
        Assert.NotEmpty(cityWithMarkets.CityTourismMarkets);
        Assert.Contains(cityWithMarkets.CityTourismMarkets, ctm => ctm.TourismMarket.Name == "Test Market");
    }
}
