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
        "Popularity uses only genuine customer ratings, visible (non-hidden) customer reviews, " +
        "and recorded in-store sales (IsDemo = 0). Simulated/demo sales are never counted. " +
        "ratingScore = (n × average + 5 × 3.5) / (n + 5). " +
        "n is the visible review count, or the rating count when a part has ratings but no written review. " +
        "average is the mean of stored star ratings (1–5). The 3.5 prior mean and weight 5 stop a single " +
        "5-star rating from outranking many strong ratings. Parts with no ratings use the prior mean 3.5. " +
        "salesScore = 5 × units / (units + 30), where units is the sum of quantities from recorded sales only. " +
        "Parts with no recorded sales get salesScore = 0. " +
        "popularity = 0.6 × ratingScore + 0.4 × salesScore. " +
        "Ties go to the higher recorded-sales quantity, then the lower product id.";

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

    /// <summary>Recorded (non-demo) sale units only.</summary>
    public static int UnitsSold(int recordedSales) => Math.Max(0, recordedSales);

    public static double Score(double averageRating, int ratingCount, int reviewCount, int recordedSales)
    {
        return (RatingWeight * RatingScore(averageRating, ratingCount, reviewCount))
            + (SalesWeight * SalesScore(UnitsSold(recordedSales)));
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
        Func<T, int> recordedSales,
        Func<T, int> productId)
    {
        return products
            .OrderByDescending(score)
            .ThenByDescending(recordedSales)
            .ThenBy(productId);
    }
}
