using FWH.MarketingApi.Data;
using FWH.MarketingApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FWH.MarketingApi.Controllers;

/// <summary>
/// API controller for retrieving business marketing data (themes, coupons, menus, news).
/// Implements TR-API-002: Marketing endpoints.
/// </summary>
/// <remarks>
/// This controller provides all marketing-related endpoints as specified in TR-API-002:
/// - Complete marketing data retrieval
/// - Theme, coupons, menu, and news endpoints
/// - Nearby business discovery
/// </remarks>
[ApiController]
[Route("api/[controller]")]
internal class MarketingController : ControllerBase
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
    /// Implements TR-API-002: GET /api/marketing/{businessId}.
    /// </summary>
    /// <param name="businessId">Business ID</param>
    /// <returns>Complete marketing data including theme, coupons, menu items, and news</returns>
    /// <exception cref="NotFoundResult">Thrown when business is not found or not subscribed</exception>
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
            .FirstOrDefaultAsync(b => b.Id == businessId && b.IsSubscribed).ConfigureAwait(false);

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

        _logger.LogDebug("Retrieved marketing data for business {BusinessId}", businessId);
        return Ok(response);
    }

    /// <summary>
    /// Get active theme for a business.
    /// Implements TR-API-002: GET /api/marketing/{businessId}/theme.
    /// </summary>
    /// <param name="businessId">Business ID</param>
    /// <returns>Active business theme</returns>
    /// <exception cref="NotFoundResult">Thrown when theme is not found or business is not subscribed</exception>
    [HttpGet("{businessId}/theme")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BusinessTheme>> GetTheme(long businessId)
    {
        var theme = await _context.BusinessThemes
            .Include(t => t.Business)
            .FirstOrDefaultAsync(t => t.BusinessId == businessId && t.IsActive && t.Business.IsSubscribed).ConfigureAwait(false);

        if (theme == null)
        {
            _logger.LogWarning("Theme not found for business {BusinessId}", businessId);
            return NotFound();
        }

        return Ok(theme);
    }

    /// <summary>
    /// Get active coupons for a business with pagination.
    /// Implements TR-API-002: GET /api/marketing/{businessId}/coupons.
    /// </summary>
    /// <param name="businessId">Business ID</param>
    /// <param name="page">Page number (1-based, default: 1)</param>
    /// <param name="pageSize">Items per page (default: 20, max: 100)</param>
    /// <returns>Paginated list of active coupons for the business</returns>
    [HttpGet("{businessId}/coupons")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<Coupon>>> GetCoupons(
        long businessId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var pagination = new PaginationParameters { Page = page, PageSize = pageSize };
        pagination.Validate();

        var now = DateTimeOffset.UtcNow;
        var query = _context.Coupons
            .Include(c => c.Business)
            .Where(c => c.BusinessId == businessId
                && c.IsActive
                && c.ValidFrom <= now
                && c.ValidUntil >= now
                && c.Business.IsSubscribed
                && (c.MaxRedemptions == null || c.CurrentRedemptions < c.MaxRedemptions));

        var totalCount = await query.CountAsync().ConfigureAwait(false);
        var coupons = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip(pagination.Skip)
            .Take(pagination.Take)
            .ToListAsync().ConfigureAwait(false);

        var result = new PagedResult<Coupon>
        {
            Items = coupons,
            Page = pagination.Page,
            PageSize = pagination.PageSize,
            TotalCount = totalCount
        };

        _logger.LogDebug("Retrieved {Count} coupons (page {Page}) for business {BusinessId}",
            coupons.Count, pagination.Page, businessId);
        return Ok(result);
    }

    /// <summary>
    /// Get menu items for a business, optionally filtered by category.
    /// Implements TR-API-002: GET /api/marketing/{businessId}/menu.
    /// </summary>
    /// <param name="businessId">Business ID</param>
    /// <param name="category">Optional category filter</param>
    /// <returns>List of menu items, optionally filtered by category</returns>
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
            .ToListAsync().ConfigureAwait(false);

        _logger.LogDebug("Retrieved {Count} menu items for business {BusinessId}", menuItems.Count, businessId);
        return Ok(menuItems);
    }

    /// <summary>
    /// Get menu categories for a business.
    /// Implements TR-API-002: GET /api/marketing/{businessId}/menu/categories.
    /// </summary>
    /// <param name="businessId">Business ID</param>
    /// <returns>List of distinct menu categories</returns>
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
            .ToListAsync().ConfigureAwait(false);

        return Ok(categories);
    }

    /// <summary>
    /// Get news items for a business with pagination.
    /// Implements TR-API-002: GET /api/marketing/{businessId}/news.
    /// </summary>
    /// <param name="businessId">Business ID</param>
    /// <param name="page">Page number (1-based, default: 1)</param>
    /// <param name="pageSize">Items per page (default: 20, max: 100)</param>
    /// <returns>Paginated list of published news items for the business</returns>
    [HttpGet("{businessId}/news")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<NewsItem>>> GetNews(
        long businessId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var pagination = new PaginationParameters { Page = page, PageSize = pageSize };
        pagination.Validate();

        var now = DateTimeOffset.UtcNow;
        var query = _context.NewsItems
            .Include(n => n.Business)
            .Where(n => n.BusinessId == businessId
                && n.IsPublished
                && n.PublishedAt <= now
                && n.Business.IsSubscribed
                && (n.ExpiresAt == null || n.ExpiresAt > now));

        var totalCount = await query.CountAsync().ConfigureAwait(false);
        var newsItems = await query
            .OrderByDescending(n => n.IsFeatured)
            .ThenByDescending(n => n.PublishedAt)
            .Skip(pagination.Skip)
            .Take(pagination.Take)
            .ToListAsync().ConfigureAwait(false);

        var result = new PagedResult<NewsItem>
        {
            Items = newsItems,
            Page = pagination.Page,
            PageSize = pagination.PageSize,
            TotalCount = totalCount
        };

        _logger.LogDebug("Retrieved {Count} news items (page {Page}) for business {BusinessId}",
            newsItems.Count, pagination.Page, businessId);
        return Ok(result);
    }

    /// <summary>
    /// Find businesses near a location.
    /// Implements TR-API-002: GET /api/marketing/nearby.
    /// </summary>
    /// <param name="latitude">Latitude coordinate (-90 to 90)</param>
    /// <param name="longitude">Longitude coordinate (-180 to 180)</param>
    /// <param name="radiusMeters">Search radius in meters (default 1000, max 50000)</param>
    /// <returns>List of businesses within the specified radius</returns>
    /// <exception cref="BadRequestResult">Thrown when coordinates are invalid or radius is out of range</exception>
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

        // Try to use PostGIS spatial queries for efficient distance-based filtering
        // If PostGIS is not available (e.g., in test environments), fall back to bounding box method
        List<Business> businesses;

        try
        {
            // Use parameterized SQL with PostGIS for efficient spatial query
            // This query uses the spatial GIST index for optimal performance
            // FormattableString automatically parameterizes the values
            businesses = await _context.Businesses
                .FromSqlInterpolated($@"
                    SELECT b.*
                    FROM businesses b
                    WHERE b.is_subscribed = true
                      AND b.location_geometry IS NOT NULL
                      AND ST_DWithin(
                          b.location_geometry::geography,
                          ST_SetSRID(ST_MakePoint({longitude}, {latitude}), 4326)::geography,
                          {radiusMeters}
                      )
                    ORDER BY ST_Distance(
                        b.location_geometry::geography,
                        ST_SetSRID(ST_MakePoint({longitude}, {latitude}), 4326)::geography
                    )
                ")
                .ToListAsync().ConfigureAwait(false);

            _logger.LogDebug("Found {Count} businesses within {Radius}m of ({Lat}, {Lon}) using PostGIS spatial query",
                businesses.Count, radiusMeters, latitude, longitude);
        }
        catch (Exception ex) when (ex is Npgsql.PostgresException pgEx && (pgEx.SqlState == "0A000" || pgEx.Message.Contains("postgis", StringComparison.OrdinalIgnoreCase)) ||
                                     ex.Message.Contains("postgis", StringComparison.OrdinalIgnoreCase) ||
                                     ex.Message.Contains("ST_DWithin", StringComparison.OrdinalIgnoreCase) ||
                                     ex.Message.Contains("location_geometry", StringComparison.OrdinalIgnoreCase))
        {
            // PostGIS is not available, fall back to bounding box method
            _logger.LogWarning(ex, "PostGIS not available, falling back to bounding box query method");

            // Simple bounding box query (fallback when PostGIS is not available)
            var latDelta = radiusMeters / 111000.0; // Approximate degrees
            var lonDelta = radiusMeters / (111000.0 * Math.Cos(latitude * Math.PI / 180.0));

            var allBusinesses = await _context.Businesses
                .Where(b => b.IsSubscribed
                    && b.Latitude >= latitude - latDelta
                    && b.Latitude <= latitude + latDelta
                    && b.Longitude >= longitude - lonDelta
                    && b.Longitude <= longitude + lonDelta)
                .ToListAsync().ConfigureAwait(false);

            // Filter by actual distance
            businesses = allBusinesses
                .Select(b => new
                {
                    Business = b,
                    Distance = CalculateDistance(latitude, longitude, b.Latitude, b.Longitude)
                })
                .Where(x => x.Distance <= radiusMeters)
                .OrderBy(x => x.Distance)
                .Select(x => x.Business)
                .ToList();

            _logger.LogDebug("Found {Count} businesses within {Radius}m of ({Lat}, {Lon}) using bounding box fallback",
                businesses.Count, radiusMeters, latitude, longitude);
        }

        return Ok(businesses);
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

    /// <summary>
    /// Get marketing information for a city by name and state/country.
    /// </summary>
    /// <param name="cityName">City name</param>
    /// <param name="state">State or province (optional)</param>
    /// <param name="country">Country (optional)</param>
    /// <returns>City marketing data including theme</returns>
    [HttpGet("city")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CityMarketingResponse>> GetCityMarketing(
        [FromQuery] string cityName,
        [FromQuery] string? state = null,
        [FromQuery] string? country = null)
    {
        if (string.IsNullOrWhiteSpace(cityName))
        {
            return BadRequest("City name is required");
        }

        // Use EF.Functions.ILike for case-insensitive comparison (PostgreSQL-specific)
        // For cross-database compatibility, could use ToLower() but ILike is more efficient
        var query = _context.Cities
            .Include(c => c.Theme)
            .Where(c => c.IsActive && EF.Functions.ILike(c.Name, cityName));

        if (!string.IsNullOrWhiteSpace(state))
        {
            query = query.Where(c => EF.Functions.ILike(c.State, state));
        }

        if (!string.IsNullOrWhiteSpace(country))
        {
            query = query.Where(c => EF.Functions.ILike(c.Country, country));
        }

        var city = await query.FirstOrDefaultAsync().ConfigureAwait(false);

        if (city == null)
        {
            _logger.LogWarning("City not found: {CityName}, {State}, {Country}", cityName, state, country);
            return NotFound();
        }

        // Create theme DTO to avoid circular reference issues
        CityThemeDto? themeDto = null;
        if (city.Theme != null && city.Theme.IsActive)
        {
            themeDto = new CityThemeDto
            {
                Id = city.Theme.Id,
                CityId = city.Theme.CityId,
                ThemeName = city.Theme.ThemeName,
                PrimaryColor = city.Theme.PrimaryColor,
                SecondaryColor = city.Theme.SecondaryColor,
                AccentColor = city.Theme.AccentColor,
                BackgroundColor = city.Theme.BackgroundColor,
                TextColor = city.Theme.TextColor,
                LogoUrl = city.Theme.LogoUrl,
                BackgroundImageUrl = city.Theme.BackgroundImageUrl,
                CustomCss = city.Theme.CustomCss,
                IsActive = city.Theme.IsActive,
                CreatedAt = city.Theme.CreatedAt,
                UpdatedAt = city.Theme.UpdatedAt
            };
        }

        var response = new CityMarketingResponse
        {
            CityId = city.Id,
            CityName = city.Name,
            State = city.State,
            Country = city.Country,
            Description = city.Description,
            Website = city.Website,
            Theme = themeDto
        };

        _logger.LogDebug("Retrieved marketing data for city: {CityName}", city.Name);
        return Ok(response);
    }

    /// <summary>
    /// Get city theme by city ID.
    /// </summary>
    /// <param name="cityId">City ID</param>
    /// <returns>City theme</returns>
    [HttpGet("city/{cityId}/theme")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CityTheme>> GetCityTheme(long cityId)
    {
        var theme = await _context.CityThemes
            .Include(t => t.City)
            .FirstOrDefaultAsync(t => t.CityId == cityId && t.IsActive && t.City.IsActive).ConfigureAwait(false);

        if (theme == null)
        {
            _logger.LogWarning("Theme not found for city {CityId}", cityId);
            return NotFound();
        }

        return Ok(theme);
    }
}
