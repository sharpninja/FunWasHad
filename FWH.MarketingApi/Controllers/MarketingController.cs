using FWH.MarketingApi.Data;
using FWH.MarketingApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FWH.MarketingApi.Controllers;

/// <summary>
/// API controller for retrieving business marketing data (themes, coupons, menus, news).
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class MarketingController : ControllerBase
{
    private readonly MarketingDbContext _context;
    private readonly ILogger<MarketingController> _logger;

    public MarketingController(MarketingDbContext context, ILogger<MarketingController> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get complete marketing data for a business (theme, coupons, menu, news).
    /// </summary>
    /// <param name="businessId">Business ID</param>
    [HttpGet("{businessId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BusinessMarketingResponse>> GetBusinessMarketing(long businessId)
    {
        var business = await _context.Businesses
            .Include(b => b.Theme)
            .Include(b => b.Coupons.Where(c => c.IsActive && c.ValidFrom <= DateTimeOffset.UtcNow && c.ValidUntil >= DateTimeOffset.UtcNow))
            .Include(b => b.MenuItems.Where(m => m.IsAvailable))
            .Include(b => b.NewsItems.Where(n => n.IsPublished && n.PublishedAt <= DateTimeOffset.UtcNow))
            .FirstOrDefaultAsync(b => b.Id == businessId && b.IsSubscribed);

        if (business == null)
        {
            _logger.LogWarning("Business {BusinessId} not found or not subscribed", businessId);
            return NotFound();
        }

        var response = new BusinessMarketingResponse
        {
            BusinessId = business.Id,
            BusinessName = business.Name,
            Theme = business.Theme,
            Coupons = business.Coupons.OrderByDescending(c => c.CreatedAt).ToList(),
            MenuItems = business.MenuItems.OrderBy(m => m.Category).ThenBy(m => m.SortOrder).ToList(),
            NewsItems = business.NewsItems.OrderByDescending(n => n.PublishedAt).Take(10).ToList()
        };

        _logger.LogInformation("Retrieved marketing data for business {BusinessId}", businessId);
        return Ok(response);
    }

    /// <summary>
    /// Get active theme for a business.
    /// </summary>
    /// <param name="businessId">Business ID</param>
    [HttpGet("{businessId}/theme")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BusinessTheme>> GetTheme(long businessId)
    {
        var theme = await _context.BusinessThemes
            .Include(t => t.Business)
            .FirstOrDefaultAsync(t => t.BusinessId == businessId && t.IsActive && t.Business.IsSubscribed);

        if (theme == null)
        {
            _logger.LogWarning("Theme not found for business {BusinessId}", businessId);
            return NotFound();
        }

        return Ok(theme);
    }

    /// <summary>
    /// Get active coupons for a business.
    /// </summary>
    /// <param name="businessId">Business ID</param>
    [HttpGet("{businessId}/coupons")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<Coupon>>> GetCoupons(long businessId)
    {
        var now = DateTimeOffset.UtcNow;
        var coupons = await _context.Coupons
            .Include(c => c.Business)
            .Where(c => c.BusinessId == businessId 
                && c.IsActive 
                && c.ValidFrom <= now 
                && c.ValidUntil >= now
                && c.Business.IsSubscribed
                && (c.MaxRedemptions == null || c.CurrentRedemptions < c.MaxRedemptions))
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        _logger.LogInformation("Retrieved {Count} coupons for business {BusinessId}", coupons.Count, businessId);
        return Ok(coupons);
    }

    /// <summary>
    /// Get menu items for a business, optionally filtered by category.
    /// </summary>
    /// <param name="businessId">Business ID</param>
    /// <param name="category">Optional category filter</param>
    [HttpGet("{businessId}/menu")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<MenuItem>>> GetMenu(long businessId, [FromQuery] string? category = null)
    {
        var query = _context.MenuItems
            .Include(m => m.Business)
            .Where(m => m.BusinessId == businessId && m.IsAvailable && m.Business.IsSubscribed);

        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(m => m.Category == category);
        }

        var menuItems = await query
            .OrderBy(m => m.Category)
            .ThenBy(m => m.SortOrder)
            .ThenBy(m => m.Name)
            .ToListAsync();

        _logger.LogInformation("Retrieved {Count} menu items for business {BusinessId}", menuItems.Count, businessId);
        return Ok(menuItems);
    }

    /// <summary>
    /// Get menu categories for a business.
    /// </summary>
    /// <param name="businessId">Business ID</param>
    [HttpGet("{businessId}/menu/categories")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<string>>> GetMenuCategories(long businessId)
    {
        var categories = await _context.MenuItems
            .Include(m => m.Business)
            .Where(m => m.BusinessId == businessId && m.IsAvailable && m.Business.IsSubscribed)
            .Select(m => m.Category)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();

        return Ok(categories);
    }

    /// <summary>
    /// Get news items for a business.
    /// </summary>
    /// <param name="businessId">Business ID</param>
    /// <param name="limit">Maximum number of news items to return (default 10)</param>
    [HttpGet("{businessId}/news")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<NewsItem>>> GetNews(long businessId, [FromQuery] int limit = 10)
    {
        var now = DateTimeOffset.UtcNow;
        var newsItems = await _context.NewsItems
            .Include(n => n.Business)
            .Where(n => n.BusinessId == businessId 
                && n.IsPublished 
                && n.PublishedAt <= now
                && n.Business.IsSubscribed
                && (n.ExpiresAt == null || n.ExpiresAt > now))
            .OrderByDescending(n => n.IsFeatured)
            .ThenByDescending(n => n.PublishedAt)
            .Take(Math.Min(limit, 50))
            .ToListAsync();

        _logger.LogInformation("Retrieved {Count} news items for business {BusinessId}", newsItems.Count, businessId);
        return Ok(newsItems);
    }

    /// <summary>
    /// Find businesses near a location.
    /// </summary>
    /// <param name="latitude">Latitude</param>
    /// <param name="longitude">Longitude</param>
    /// <param name="radiusMeters">Search radius in meters (default 1000)</param>
    [HttpGet("nearby")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<Business>>> GetNearbyBusinesses(
        [FromQuery] double latitude,
        [FromQuery] double longitude,
        [FromQuery] int radiusMeters = 1000)
    {
        if (latitude < -90 || latitude > 90)
        {
            return BadRequest("Latitude must be between -90 and 90");
        }

        if (longitude < -180 || longitude > 180)
        {
            return BadRequest("Longitude must be between -180 and 180");
        }

        if (radiusMeters <= 0 || radiusMeters > 50000)
        {
            return BadRequest("Radius must be between 1 and 50000 meters");
        }

        // Simple bounding box query (for production, use PostGIS or similar)
        var latDelta = radiusMeters / 111000.0; // Approximate degrees
        var lonDelta = radiusMeters / (111000.0 * Math.Cos(latitude * Math.PI / 180.0));

        var businesses = await _context.Businesses
            .Where(b => b.IsSubscribed
                && b.Latitude >= latitude - latDelta
                && b.Latitude <= latitude + latDelta
                && b.Longitude >= longitude - lonDelta
                && b.Longitude <= longitude + lonDelta)
            .ToListAsync();

        // Filter by actual distance
        var nearbyBusinesses = businesses
            .Select(b => new
            {
                Business = b,
                Distance = CalculateDistance(latitude, longitude, b.Latitude, b.Longitude)
            })
            .Where(x => x.Distance <= radiusMeters)
            .OrderBy(x => x.Distance)
            .Select(x => x.Business)
            .ToList();

        _logger.LogInformation("Found {Count} businesses within {Radius}m of ({Lat}, {Lon})",
            nearbyBusinesses.Count, radiusMeters, latitude, longitude);

        return Ok(nearbyBusinesses);
    }

    private static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double earthRadiusMeters = 6371000;
        var dLat = (lat2 - lat1) * Math.PI / 180.0;
        var dLon = (lon2 - lon1) * Math.PI / 180.0;

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return earthRadiusMeters * c;
    }
}
