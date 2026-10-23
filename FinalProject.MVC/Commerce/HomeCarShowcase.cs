using FinalProject.MVC.Models;

namespace FinalProject.MVC.Commerce;

/// <summary>
/// Home portfolio carousel filters. Uses catalog fields only — never invents
/// condition, stock, or trending from year or other proxies.
/// </summary>
public static class HomeCarShowcase
{
    public const string ActiveStatus = "Active";

    public static IReadOnlyList<Car> ShowcaseCars(IEnumerable<Car> cars) =>
        cars
            .Where(car =>
                !string.IsNullOrWhiteSpace(car.ImagePath)
                && car.ImagePath.Contains("/assets/images/catalog/", StringComparison.OrdinalIgnoreCase))
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
    /// IsFeatured is not used: the catalog seeds every car with IsFeatured = false.
    /// </summary>
    public static IReadOnlyList<Car> TrendingCars(IEnumerable<Car> cars) =>
        cars
            .OrderByDescending(car => car.Year)
            .ThenBy(car => car.Name, StringComparer.OrdinalIgnoreCase)
            .Take(6)
            .ToList();

    /// <summary>
    /// Original Featured / Premium carousel: Mercedes-Benz first, then newest year, then name.
    /// </summary>
    public static IReadOnlyList<Car> PremiumCars(IEnumerable<Car> cars) =>
        cars
            .OrderByDescending(car => car.Make.Contains("Mercedes", StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(car => car.Year)
            .ThenBy(car => car.Name, StringComparer.OrdinalIgnoreCase)
            .Take(6)
            .ToList();

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
