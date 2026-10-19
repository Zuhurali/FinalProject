namespace FinalProject.DTOs;

public class CarListQuery
{
    public const int DefaultPageSize = 10;

    public const int MaxPageSize = 100;

    public string? Search { get; set; }

    public int? BrandId { get; set; }

    public int? CategoryId { get; set; }

    public string? Model { get; set; }

    public decimal? MinPrice { get; set; }

    public decimal? MaxPrice { get; set; }

    public int? MinYear { get; set; }

    public int? MaxYear { get; set; }

    public string? Fuel { get; set; }

    public string? Transmission { get; set; }

    public bool? IsNew { get; set; }

    public string? Status { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = DefaultPageSize;

    public string? Validate()
    {
        if (Page < 1)
        {
            return "page must be greater than zero.";
        }

        if (PageSize < 1 || PageSize > MaxPageSize)
        {
            return $"pageSize must be between 1 and {MaxPageSize}.";
        }

        if (BrandId is <= 0)
        {
            return "brandId must be greater than zero.";
        }

        if (CategoryId is <= 0)
        {
            return "categoryId must be greater than zero.";
        }

        if (MinPrice is < 0 || MaxPrice is < 0)
        {
            return "Price filters cannot be negative.";
        }

        if (MinPrice is decimal minPrice && MaxPrice is decimal maxPrice && minPrice > maxPrice)
        {
            return "minPrice cannot be greater than maxPrice.";
        }

        if (MinYear is < 0 || MaxYear is < 0)
        {
            return "Year filters cannot be negative.";
        }

        if (MinYear is int minYear && MaxYear is int maxYear && minYear > maxYear)
        {
            return "minYear cannot be greater than maxYear.";
        }

        return null;
    }
}

public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();

    public int CurrentPage { get; set; }

    public int PageSize { get; set; }

    public int TotalCount { get; set; }

    public int TotalPages { get; set; }
}
