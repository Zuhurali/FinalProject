using FinalProject.MVC.Models;

namespace FinalProject.MVC.Data;

public class CarListingQuery
{
    public string? Category { get; set; }

    public string? Make { get; set; }

    public string? Model { get; set; }

    public string? PriceRange { get; set; }

    public string? Fuel { get; set; }

    public string? Transmission { get; set; }

    public int Page { get; set; } = 1;
}

public class CarListingResult
{
    public const int PageSize = 6;

    public IReadOnlyList<Car> Cars { get; set; } = Array.Empty<Car>();

    public CarListingQuery Query { get; set; } = new();

    public int Page { get; set; } = 1;

    public int TotalCount { get; set; }

    public int TotalPages { get; set; }

    public string? CategorySlug { get; set; }

    public string? CategoryDisplay { get; set; }

    public bool CategoryRequested { get; set; }

    public string? ErrorMessage { get; set; }

    public IReadOnlyList<string> Makes { get; set; } = Array.Empty<string>();

    public IReadOnlyList<string> Models { get; set; } = Array.Empty<string>();

    public bool HasActiveFilters =>
        CategoryRequested
        || HasValue(Query.Make)
        || HasValue(Query.Model)
        || HasValue(Query.PriceRange)
        || HasValue(Query.Fuel)
        || HasValue(Query.Transmission);

    private static bool HasValue(string? value)
    {
        return !string.IsNullOrWhiteSpace(value) && !value.Equals("All", StringComparison.OrdinalIgnoreCase);
    }
}

public static class CarListingFilter
{
    public static IReadOnlyList<Car> Filter(IEnumerable<Car> source, CarListingQuery query)
    {
        IEnumerable<Car> cars = source;

        if (!string.IsNullOrWhiteSpace(query.Category))
        {
            var category = CarCategories.Resolve(query.Category);
            if (category == null)
            {
                return Array.Empty<Car>();
            }

            cars = cars.Where(car => car.Category.Equals(category.Value, StringComparison.OrdinalIgnoreCase));
        }

        if (HasValue(query.Make))
        {
            cars = cars.Where(car => car.Make.Equals(query.Make, StringComparison.OrdinalIgnoreCase));
        }

        if (HasValue(query.Model))
        {
            cars = cars.Where(car => car.Model.Equals(query.Model, StringComparison.OrdinalIgnoreCase));
        }

        if (HasValue(query.PriceRange))
        {
            var (minPrice, maxPrice) = ParsePriceRange(query.PriceRange!);
            cars = cars.Where(car => car.Price >= minPrice && car.Price <= maxPrice);
        }

        if (HasValue(query.Fuel))
        {
            cars = cars.Where(car => car.Fuel.Equals(query.Fuel, StringComparison.OrdinalIgnoreCase));
        }

        if (HasValue(query.Transmission))
        {
            cars = cars.Where(car => car.Transmission.Equals(query.Transmission, StringComparison.OrdinalIgnoreCase));
        }

        return cars.ToList();
    }

    public static CarListingResult Page(IReadOnlyList<Car> matches, CarListingQuery query, IReadOnlyList<Car> catalog)
    {
        var requestedCategory = CarCategories.Resolve(query.Category);
        var pageSize = CarListingResult.PageSize;
        var totalPages = matches.Count == 0 ? 0 : (int)Math.Ceiling(matches.Count / (double)pageSize);
        var page = query.Page < 1 ? 1 : query.Page;
        if (totalPages > 0 && page > totalPages)
        {
            page = totalPages;
        }

        IReadOnlyList<Car> cars = totalPages == 0
            ? Array.Empty<Car>()
            : matches.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return new CarListingResult
        {
            Cars = cars,
            Query = query,
            Page = page,
            TotalCount = matches.Count,
            TotalPages = totalPages,
            CategoryRequested = !string.IsNullOrWhiteSpace(query.Category),
            CategorySlug = requestedCategory?.Slug ?? query.Category,
            CategoryDisplay = requestedCategory?.DisplayName ?? query.Category,
            Makes = catalog.Select(car => car.Make).Where(make => !string.IsNullOrWhiteSpace(make)).Distinct().OrderBy(make => make).ToList(),
            Models = catalog.Select(car => car.Model).Where(model => !string.IsNullOrWhiteSpace(model)).Distinct().OrderBy(model => model).ToList()
        };
    }

    public static (decimal Min, decimal Max) ParsePriceRange(string priceRange)
    {
        if (priceRange.Equals("70000+", StringComparison.OrdinalIgnoreCase))
        {
            return (70000, decimal.MaxValue);
        }

        var parts = priceRange.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 2
            && decimal.TryParse(parts[0], out var min)
            && decimal.TryParse(parts[1], out var max))
        {
            return (min, max);
        }

        return (0, decimal.MaxValue);
    }

    private static bool HasValue(string? value)
    {
        return !string.IsNullOrWhiteSpace(value) && !value.Equals("All", StringComparison.OrdinalIgnoreCase);
    }
}
