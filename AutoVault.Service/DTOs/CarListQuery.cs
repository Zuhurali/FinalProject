namespace FinalProject.DTOs;

public class CarListQueryDto
{
    public const int DefaultPageSize = 10;

    public const int MaxPageSize = 100;

    public static readonly HashSet<string> AllowedSorts = new(StringComparer.OrdinalIgnoreCase)
    {
        "latest",
        "price-asc",
        "price-desc",
        "year-desc",
        "year-asc"
    };

    public string? Search { get; set; }

    public int? BrandId { get; set; }

    public int? CategoryId { get; set; }

    public string? Model { get; set; }

    public decimal? MinPrice { get; set; }

    public decimal? MaxPrice { get; set; }

    public int? MinYear { get; set; }

    public int? MaxYear { get; set; }

    public int? MinMileage { get; set; }

    public int? MaxMileage { get; set; }

    public int? Seats { get; set; }

    public List<int> SeatCounts { get; set; } = new();

    public string? Fuel { get; set; }

    public string? Transmission { get; set; }

    public bool? IsNew { get; set; }

    public string? Status { get; set; }

    /// <summary>
    /// Optional server-side sort. When null/empty, callers keep the legacy Brand/Model/Year order.
    /// Allowed: latest, price-asc, price-desc, year-desc, year-asc.
    /// </summary>
    public string? Sort { get; set; }

    /// <summary>
    /// When true, search also matches color, features, and category.
    /// CarsThree sets this. Other listings keep make, model, and description only.
    /// </summary>
    public bool IncludeListingText { get; set; }

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

        if (MinMileage is < 0 || MaxMileage is < 0)
        {
            return "Mileage filters cannot be negative.";
        }

        if (MinMileage is int minMileage && MaxMileage is int maxMileage && minMileage > maxMileage)
        {
            return "minMileage cannot be greater than maxMileage.";
        }

        if (Seats is int seats && (seats < 1 || seats > 12))
        {
            return "seats must be between 1 and 12.";
        }

        if (SeatCounts != null && SeatCounts.Any(seat => seat < 1 || seat > 12))
        {
            return "seats must be between 1 and 12.";
        }

        if (!string.IsNullOrWhiteSpace(Sort) && !AllowedSorts.Contains(Sort.Trim()))
        {
            return "sort must be one of: latest, price-asc, price-desc, year-desc, year-asc.";
        }

        return null;
    }

    public List<int> SelectedSeats()
    {
        var selected = new List<int>();
        if (Seats is >= 1 and <= 12)
        {
            selected.Add(Seats.Value);
        }

        if (SeatCounts != null)
        {
            selected.AddRange(SeatCounts.Where(seat => seat is >= 1 and <= 12));
        }
        return selected.Distinct().ToList();
    }
}

public class PagedResultDto<T>
{
    public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();

    public int CurrentPage { get; set; }

    public int PageSize { get; set; }

    public int TotalCount { get; set; }

    public int TotalPages { get; set; }
}
