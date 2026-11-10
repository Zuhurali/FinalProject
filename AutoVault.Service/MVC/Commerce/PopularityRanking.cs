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
        "Popular Items ranks by genuine recorded in-store sales first: the sum of quantities " +
        "from InPersonSales where IsDemo = 0 and VoidedAt is null. Voided sales and purchase requests are not counted. " +
        "When recorded sales are equal, the spare part with more ProductFavorites comes first. " +
        "If both counts match, the lower product id comes first. Latest Items stays newest CreatedAt, then higher product id.";

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
        Func<T, int> recordedSales,
        Func<T, int> favoriteCount,
        Func<T, int> productId)
    {
        return products
            .OrderByDescending(recordedSales)
            .ThenByDescending(favoriteCount)
            .ThenBy(productId);
    }
}
