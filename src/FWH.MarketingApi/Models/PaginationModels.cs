namespace FWH.MarketingApi.Models;

/// <summary>
/// Pagination parameters for list endpoints.
/// </summary>
public class PaginationParameters
{
    /// <summary>
    /// Page number (1-based). Defaults to 1.
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Number of items per page. Defaults to 20, maximum 100.
    /// </summary>
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Validates and normalizes pagination parameters.
    /// </summary>
    public void Validate()
    {
        if (Page < 1) Page = 1;
        if (PageSize < 1) PageSize = 20;
        if (PageSize > 100) PageSize = 100;
    }

    /// <summary>
    /// Gets the number of items to skip for the current page.
    /// </summary>
    public int Skip => (Page - 1) * PageSize;

    /// <summary>
    /// Gets the number of items to take for the current page.
    /// </summary>
    public int Take => PageSize;
}

/// <summary>
/// Paginated result with metadata.
/// </summary>
/// <typeparam name="T">Type of items in the result</typeparam>
public class PagedResult<T>
{
    /// <summary>
    /// Items for the current page.
    /// </summary>
    public required List<T> Items { get; init; }

    /// <summary>
    /// Current page number (1-based).
    /// </summary>
    public int Page { get; init; }

    /// <summary>
    /// Number of items per page.
    /// </summary>
    public int PageSize { get; init; }

    /// <summary>
    /// Total number of items across all pages.
    /// </summary>
    public int TotalCount { get; init; }

    /// <summary>
    /// Total number of pages.
    /// </summary>
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    /// <summary>
    /// Whether there is a previous page.
    /// </summary>
    public bool HasPreviousPage => Page > 1;

    /// <summary>
    /// Whether there is a next page.
    /// </summary>
    public bool HasNextPage => Page < TotalPages;
}
