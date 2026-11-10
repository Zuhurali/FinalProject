using FinalProject.MVC.Models;

namespace FinalProject.MVC.Commerce;

/// <summary>
/// Home portfolio carousel filters. Uses catalog fields only — never invents
/// condition, stock, or trending from year or other proxies.
/// </summary>
public static class HomeCarShowcase
{
    public const string ActiveStatus = "Active";

    /// <summary>
    /// Seeded catalog assets and Admin vehicle uploads under wwwroot.
    /// External URLs and path traversal are rejected.
    /// </summary>
    private static readonly string[] ShowcaseImagePrefixes =
    [
        "/assets/images/catalog/",
        "/assets/images/portfolio/cars/uploads/"
    ];

    public static bool IsShowcaseImagePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var text = path.Trim();
        if (text.Length > 500
            || text.Contains("..", StringComparison.Ordinal)
            || text.Contains('\\'))
        {
            return false;
        }

        if (!(text.StartsWith("~/", StringComparison.Ordinal) || text.StartsWith("/", StringComparison.Ordinal)))
        {
            return false;
        }

        var normalized = text.StartsWith("~/", StringComparison.Ordinal) ? text[1..] : text;
        return ShowcaseImagePrefixes.Any(prefix =>
            normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }

    public static IReadOnlyList<Car> ShowcaseCars(IEnumerable<Car> cars) =>
        cars
            .Where(car => IsShowcaseImagePath(car.ImagePath))
            .GroupBy(car => car.Id)
            .Select(group => group.First())
            .ToList();

    public static IReadOnlyList<Car> NewCars(IEnumerable<Car> cars) =>
        cars.Where(car => car.IsNew).ToList();

    public static IReadOnlyList<Car> UsedCars(IEnumerable<Car> cars) =>
        cars.Where(car => !car.IsNew).ToList();

    public const int HomeUsedCarouselSize = 6;

    /// <summary>
    /// Home Used Cars tab only. Up to six distinct active used cars, shuffled once per page render.
    /// </summary>
    public static IReadOnlyList<Car> HomeUsedCarousel(IEnumerable<Car> cars, Random? random = null)
    {
        var eligible = cars
            .Where(car => !car.IsNew && IsInStock(car))
            .GroupBy(car => car.Id)
            .Select(group => group.First())
            .ToList();

        if (eligible.Count <= HomeUsedCarouselSize)
        {
            return eligible;
        }

        var rng = random ?? Random.Shared;
        for (var index = eligible.Count - 1; index > 0; index--)
        {
            var swap = rng.Next(index + 1);
            (eligible[index], eligible[swap]) = (eligible[swap], eligible[index]);
        }

        return eligible.Take(HomeUsedCarouselSize).ToList();
    }

    /// <summary>
    /// Newest catalog vehicles, matching the original Home carousel.
    /// </summary>
    public static IReadOnlyList<Car> TrendingCars(IEnumerable<Car> cars) =>
        cars
            .OrderByDescending(car => car.Year)
            .ThenBy(car => car.Name, StringComparer.OrdinalIgnoreCase)
            .Take(6)
            .ToList();

    /// <summary>
    /// Homepage Featured carousel. When any Active listing has <see cref="Car.IsFeatured"/>,
    /// only those cars are shown (so Admin "Show on homepage" is a visible change).
    /// When none are featured, fall back to the Mercedes / newest-year premium heuristic.
    /// </summary>
    public static IReadOnlyList<Car> PremiumCars(IEnumerable<Car> cars)
    {
        var eligible = cars
            .Where(car => IsShowcaseImagePath(car.ImagePath))
            .GroupBy(car => car.Id)
            .Select(group => group.First())
            .ToList();

        var featured = eligible.Where(car => car.IsFeatured).ToList();
        if (featured.Count > 0)
        {
            return featured
                .OrderByDescending(car => car.Year)
                .ThenBy(car => car.Name, StringComparer.OrdinalIgnoreCase)
                .Take(6)
                .ToList();
        }

        return eligible
            .OrderByDescending(car => car.Make.Contains("Mercedes", StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(car => car.Year)
            .ThenBy(car => car.Name, StringComparer.OrdinalIgnoreCase)
            .Take(6)
            .ToList();
    }

    /// <summary>
    /// Available for sale. Catalog status is Active, Sold, or Draft.
    /// Active is the listing state counted as available inventory; there is no separate quantity field.
    /// </summary>
    public static IReadOnlyList<Car> InStockCars(IEnumerable<Car> cars) =>
        cars.Where(IsInStock).ToList();

    public static bool IsInStock(Car car) =>
        string.Equals(car.Status, ActiveStatus, StringComparison.OrdinalIgnoreCase);

    public static string ConditionBadge(Car car) => car.IsNew ? "NEW" : "USED";

    public static string StockBadge(Car car) => IsInStock(car) ? "IN STOCK" : "OUT OF STOCK";
}
