using FWH.MarketingApi.Data;
using FWH.MarketingApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace FWH.MarketingApi.Tests;

/// <summary>
/// Base class for controller unit tests.
/// Provides in-memory database setup and common test utilities.
/// </summary>
public abstract class ControllerTestBase : IDisposable
{
    // Internal property to match internal MarketingDbContext (accessible via InternalsVisibleTo)
    internal MarketingDbContext DbContext { get; }
    protected ILogger<T> CreateLogger<T>() => Substitute.For<ILogger<T>>();

    protected ControllerTestBase()
    {
        var options = new DbContextOptionsBuilder<MarketingDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        DbContext = new TestMarketingDbContext(options);
    }

    protected void SeedTestBusiness(long businessId = 1)
    {
        var now = DateTimeOffset.UtcNow;
        var business = new Business
        {
            Id = businessId,
            Name = "Test Business",
            Address = "123 Main St",
            Latitude = 37.7749,
            Longitude = -122.4194,
            IsSubscribed = true,
            CreatedAt = now
        };

        DbContext.Businesses.Add(business);
        DbContext.SaveChanges();
    }

    protected void SeedTestBusinessWithTheme(long businessId = 1)
    {
        var now = DateTimeOffset.UtcNow;
        var business = new Business
        {
            Id = businessId,
            Name = "Test Cafe",
            Address = "123 Main St",
            Latitude = 37.7749,
            Longitude = -122.4194,
            IsSubscribed = true,
            CreatedAt = now
        };

        var theme = new BusinessTheme
        {
            Id = 1,
            BusinessId = businessId,
            Business = business,
            ThemeName = "Test Theme",
            PrimaryColor = "#FF0000",
            IsActive = true,
            CreatedAt = now
        };
        business.Theme = theme;

        DbContext.Businesses.Add(business);
        DbContext.BusinessThemes.Add(theme);
        DbContext.SaveChanges();
    }

    protected void SeedTestBusinessWithCoupons(long businessId = 1)
    {
        var now = DateTimeOffset.UtcNow;
        var business = new Business
        {
            Id = businessId,
            Name = "Test Cafe",
            Address = "123 Main St",
            Latitude = 37.7749,
            Longitude = -122.4194,
            IsSubscribed = true,
            CreatedAt = now
        };

        var coupon = new Coupon
        {
            Id = 1,
            BusinessId = businessId,
            Business = business,
            Title = "Test Coupon",
            Description = "10% off",
            IsActive = true,
            ValidFrom = now.AddDays(-1),
            ValidUntil = now.AddDays(30),
            CurrentRedemptions = 0,
            MaxRedemptions = null,
            CreatedAt = now
        };
        business.Coupons.Add(coupon);

        DbContext.Businesses.Add(business);
        DbContext.Coupons.Add(coupon);
        DbContext.SaveChanges();
    }

    protected void SeedTestBusinessWithMenuItems(long businessId = 1)
    {
        var now = DateTimeOffset.UtcNow;
        var business = new Business
        {
            Id = businessId,
            Name = "Test Cafe",
            Address = "123 Main St",
            Latitude = 37.7749,
            Longitude = -122.4194,
            IsSubscribed = true,
            CreatedAt = now
        };

        var menuItem = new MenuItem
        {
            Id = 1,
            BusinessId = businessId,
            Business = business,
            Name = "Test Item",
            Category = "Drinks",
            Price = 5.99m,
            IsAvailable = true,
            SortOrder = 0,
            CreatedAt = now
        };
        business.MenuItems.Add(menuItem);

        DbContext.Businesses.Add(business);
        DbContext.MenuItems.Add(menuItem);
        DbContext.SaveChanges();
    }

    protected void SeedTestBusinessWithNewsItems(long businessId = 1)
    {
        var now = DateTimeOffset.UtcNow;
        var business = new Business
        {
            Id = businessId,
            Name = "Test Cafe",
            Address = "123 Main St",
            Latitude = 37.7749,
            Longitude = -122.4194,
            IsSubscribed = true,
            CreatedAt = now
        };

        var newsItem = new NewsItem
        {
            Id = 1,
            BusinessId = businessId,
            Business = business,
            Title = "Test News",
            Content = "Test content",
            IsPublished = true,
            PublishedAt = now.AddDays(-1),
            ExpiresAt = null,
            CreatedAt = now
        };
        business.NewsItems.Add(newsItem);

        DbContext.Businesses.Add(business);
        DbContext.NewsItems.Add(newsItem);
        DbContext.SaveChanges();
    }

    protected void SeedCompleteTestBusiness(long businessId = 1)
    {
        var now = DateTimeOffset.UtcNow;
        var business = new Business
        {
            Id = businessId,
            Name = "Test Cafe",
            Address = "123 Main St",
            Latitude = 37.7749,
            Longitude = -122.4194,
            IsSubscribed = true,
            CreatedAt = now
        };

        // Add theme
        var theme = new BusinessTheme
        {
            Id = 1,
            BusinessId = businessId,
            Business = business,
            ThemeName = "Test Theme",
            PrimaryColor = "#FF0000",
            IsActive = true,
            CreatedAt = now
        };
        business.Theme = theme;

        // Add coupon
        var coupon = new Coupon
        {
            Id = 1,
            BusinessId = businessId,
            Business = business,
            Title = "Test Coupon",
            Description = "10% off",
            IsActive = true,
            ValidFrom = now.AddDays(-1),
            ValidUntil = now.AddDays(30),
            CurrentRedemptions = 0,
            MaxRedemptions = null,
            CreatedAt = now
        };
        business.Coupons.Add(coupon);

        // Add menu item
        var menuItem = new MenuItem
        {
            Id = 1,
            BusinessId = businessId,
            Business = business,
            Name = "Test Item",
            Category = "Drinks",
            Price = 5.99m,
            IsAvailable = true,
            SortOrder = 0,
            CreatedAt = now
        };
        business.MenuItems.Add(menuItem);

        // Add news item
        var newsItem = new NewsItem
        {
            Id = 1,
            BusinessId = businessId,
            Business = business,
            Title = "Test News",
            Content = "Test content",
            IsPublished = true,
            PublishedAt = now.AddDays(-1),
            ExpiresAt = null,
            CreatedAt = now
        };
        business.NewsItems.Add(newsItem);

        DbContext.Businesses.Add(business);
        DbContext.BusinessThemes.Add(theme);
        DbContext.Coupons.Add(coupon);
        DbContext.MenuItems.Add(menuItem);
        DbContext.NewsItems.Add(newsItem);
        DbContext.SaveChanges();
    }

    protected void SeedTestCityWithTheme(long cityId = 1)
    {
        var now = DateTimeOffset.UtcNow;
        var city = new City
        {
            Id = cityId,
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
            CityId = cityId,
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

        DbContext.Cities.Add(city);
        DbContext.CityThemes.Add(theme);
        DbContext.SaveChanges();
    }

    protected void SeedTestCityWithoutTheme(long cityId = 2)
    {
        var now = DateTimeOffset.UtcNow;
        var city = new City
        {
            Id = cityId,
            Name = "Seattle",
            State = "Washington",
            Country = "USA",
            Latitude = 47.6062,
            Longitude = -122.3321,
            Description = "The Emerald City",
            IsActive = true,
            CreatedAt = now
        };

        DbContext.Cities.Add(city);
        DbContext.SaveChanges();
    }

    protected void SeedTestCityWithInactiveTheme(long cityId = 3)
    {
        var now = DateTimeOffset.UtcNow;
        var city = new City
        {
            Id = cityId,
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
            CityId = cityId,
            City = city,
            ThemeName = "Portland Theme",
            PrimaryColor = "#00AA00",
            IsActive = false,
            CreatedAt = now
        };
        city.Theme = inactiveTheme;

        DbContext.Cities.Add(city);
        DbContext.CityThemes.Add(inactiveTheme);
        DbContext.SaveChanges();
    }

    protected IFormFile CreateMockFormFile(byte[] content, string fileName, string contentType)
    {
        var file = Substitute.For<IFormFile>();
        file.FileName.Returns(fileName);
        file.ContentType.Returns(contentType);
        file.Length.Returns(content.Length);
        file.OpenReadStream().Returns(new MemoryStream(content));
        return file;
    }

    public void Dispose()
    {
        DbContext?.Dispose();
    }
}
