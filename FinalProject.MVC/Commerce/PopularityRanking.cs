namespace FinalProject.MVC.Commerce;

/// <summary>
/// Deterministic popularity ranking for showroom parts.
/// Customers never see <see cref="Score"/>. Administrators can read <see cref="Explanation"/>.
/// </summary>
public static class PopularityRanking
{
    public const double PriorMean = 3.5;
    public const int PriorWeight = 5;
    public const int SalesPrior = 30;
    public const double RatingWeight = 0.6;
    public const double SalesWeight = 0.4;

    public const string Explanation =
        "Popularity uses genuine customer ratings, visible customer reviews, and stored in-store sale quantities. " +
        "Demonstration ratings and reviews are excluded. " +
        "ratingScore = (n * average + 5 * 3.5) / (n + 5). " +
        "n is the visible review count, or the rating count when a part has ratings but no written review. " +
        "average is the mean of stored star ratings. 3.5 is the prior mean and 5 is the prior weight, " +
        "so one 5-star review cannot outrank many strong reviews. " +
        "salesScore = 5 * units / (units + 30), where units is the sum of stored sale quantities. " +
        "popularity = 0.6 * ratingScore + 0.4 * salesScore. " +
        "Ties go to the higher sales count, then the lower product id. " +
        "Rows marked as demo sales are simulated presentation data, not verified sales.";

    public static int SampleSize(int ratingCount, int reviewCount)
    {
        return reviewCount > 0 ? reviewCount : Math.Max(0, ratingCount);
    }

    public static double RatingScore(double averageRating, int ratingCount, int reviewCount)
    {
        var sample = SampleSize(ratingCount, reviewCount);
        if (sample == 0 || ratingCount <= 0)
        {
            return PriorMean;
        }

        var average = Math.Clamp(averageRating, 1, 5);
        return ((sample * average) + (PriorWeight * PriorMean)) / (sample + PriorWeight);
    }

    public static double SalesScore(int unitsSold)
    {
        if (unitsSold <= 0)
        {
            return 0;
        }

        return 5d * unitsSold / (unitsSold + SalesPrior);
    }

    public static int UnitsSold(int realSales, int demoSales)
    {
        return Math.Max(0, realSales) + Math.Max(0, demoSales);
    }

    public static double Score(double averageRating, int ratingCount, int reviewCount, int realSales, int demoSales)
    {
        return (RatingWeight * RatingScore(averageRating, ratingCount, reviewCount))
            + (SalesWeight * SalesScore(UnitsSold(realSales, demoSales)));
    }

    public static IEnumerable<T> ByLatest<T>(IEnumerable<T> products, Func<T, DateTime> createdAt, Func<T, int> productId)
    {
        return products
            .OrderByDescending(createdAt)
            .ThenByDescending(productId);
    }

    public static IEnumerable<T> ByPopularity<T>(
        IEnumerable<T> products,
        Func<T, double> score,
        Func<T, int> unitsSold,
        Func<T, int> productId)
    {
        return products
            .OrderByDescending(score)
            .ThenByDescending(unitsSold)
            .ThenBy(productId);
    }
}
