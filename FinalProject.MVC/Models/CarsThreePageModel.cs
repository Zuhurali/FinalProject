namespace FinalProject.MVC.Models;

public sealed class CarsThreeFilterQuery
{
    public const int PageSize = 9;

    public string? Search { get; set; }

    public string? Brand { get; set; }

    public string? Model { get; set; }

    public string? Type { get; set; }

    public decimal? MinPrice { get; set; }

    public decimal? MaxPrice { get; set; }

    public int? MinYear { get; set; }

    public int? MaxYear { get; set; }

    public string? Fuel { get; set; }

    public string? Transmission { get; set; }

    public int? MinMileage { get; set; }

    public int? MaxMileage { get; set; }

    public int? Seats { get; set; }

    public string? Sort { get; set; }

    public int Page { get; set; } = 1;
}

public sealed class CarsThreePageModel
{
    public IReadOnlyList<Car> Cars { get; set; } = Array.Empty<Car>();

    public CarsThreeFilterQuery Filters { get; set; } = new();

    public int TotalCount { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = CarsThreeFilterQuery.PageSize;

    public int TotalPages { get; set; }

    public bool HasMore => Page < TotalPages;

    public IReadOnlyList<string> Makes { get; set; } = Array.Empty<string>();

    public IReadOnlyList<string> Models { get; set; } = Array.Empty<string>();

    public IReadOnlyList<string> Types { get; set; } = Array.Empty<string>();

    public IReadOnlyList<string> Fuels { get; set; } = Array.Empty<string>();

    public IReadOnlyList<string> Transmissions { get; set; } = Array.Empty<string>();

    public IReadOnlyList<int> SeatsOptions { get; set; } = Array.Empty<int>();

    public decimal MinPriceBound { get; set; }

    public decimal MaxPriceBound { get; set; }

    public int MinYearBound { get; set; }

    public int MaxYearBound { get; set; }

    public string? ApiError { get; set; }
}
