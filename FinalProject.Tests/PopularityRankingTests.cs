using FinalProject.MVC.Commerce;
using Xunit;

namespace FinalProject.Tests;

public class PopularityRankingTests
{
    [Fact]
    public void One_five_star_review_does_not_outrank_many_strong_reviews()
    {
        var single = PopularityRanking.Score(5, 1, 1, 4);
        var established = PopularityRanking.Score(4.7, 10, 10, 48);

        Assert.True(established > single);
    }

    [Fact]
    public void Higher_recorded_sales_rank_higher_when_ratings_match()
    {
        var quiet = PopularityRanking.Score(4.5, 4, 4, 6);
        var busier = PopularityRanking.Score(4.5, 4, 4, 30);

        Assert.True(busier > quiet);
    }

    [Fact]
    public void Changing_recorded_sales_can_change_the_winner()
    {
        var first = PopularityRanking.Score(4.2, 4, 4, 10);
        var second = PopularityRanking.Score(4.2, 4, 4, 12);
        var secondAfterFewerSales = PopularityRanking.Score(4.2, 4, 4, 3);

        Assert.True(second > first);
        Assert.True(first > secondAfterFewerSales);
    }

    [Fact]
    public void Demo_sale_quantities_do_not_change_the_score()
    {
        // Former API took (real, demo); ranking must ignore simulated units entirely.
        var withoutActivity = PopularityRanking.Score(0, 0, 0, 0);
        var withOnlyPrior = PopularityRanking.Score(4.5, 0, 0, 0);
        Assert.Equal(PopularityRanking.PriorMean * PopularityRanking.RatingWeight, withoutActivity, 5);
        Assert.Equal(withoutActivity, withOnlyPrior, 5);

        var realOnly = PopularityRanking.Score(4.5, 4, 4, 30);
        // Passing former "demo" figures as recorded would wrongly inflate — ensure API has one sales arg.
        Assert.True(realOnly > PopularityRanking.Score(4.5, 4, 4, 0));
        Assert.Equal(0, PopularityRanking.SalesScore(0));
        Assert.Equal(0, PopularityRanking.UnitsSold(-3));
    }

    [Fact]
    public void Popular_order_breaks_ties_by_recorded_sales_then_product_id()
    {
        var products = new[]
        {
            Item(2, 4, 10, new DateTime(2026, 8, 2)),
            Item(1, 4, 10, new DateTime(2026, 8, 1)),
            Item(3, 4, 12, new DateTime(2026, 8, 3))
        };

        var ordered = PopularityRanking.ByPopularity(
            products,
            item => PopularityRanking.Score(item.Average, item.Reviews, item.Reviews, item.Sales),
            item => item.Sales,
            item => item.Id).Select(item => item.Id).ToArray();

        Assert.Equal(new[] { 3, 1, 2 }, ordered);
    }

    [Fact]
    public void Latest_order_uses_creation_date_rather_than_popularity()
    {
        var products = new[]
        {
            Item(16, 4.7, 48, new DateTime(2026, 8, 16)),
            Item(21, 4, 1, new DateTime(2026, 8, 21))
        };

        var latest = PopularityRanking.ByLatest(products, item => item.CreatedAt, item => item.Id)
            .Select(item => item.Id)
            .First();
        var popular = PopularityRanking.ByPopularity(
            products,
            item => PopularityRanking.Score(item.Average, item.Reviews, item.Reviews, item.Sales),
            item => item.Sales,
            item => item.Id).Select(item => item.Id).First();

        Assert.Equal(21, latest);
        Assert.Equal(16, popular);
    }

    [Fact]
    public void Recorded_sales_plan_ranks_high_volume_part_first_and_keeps_latest_separate()
    {
        // Explicit recorded quantities (not PopularityDemoPlan) — mirrors former demo volumes for ordering only.
        var recordedSalesById = new Dictionary<int, int>
        {
            [1] = 8, [2] = 16, [3] = 9, [4] = 11, [5] = 6, [6] = 12, [7] = 20, [8] = 4,
            [9] = 10, [10] = 13, [11] = 15, [12] = 7, [13] = 6, [14] = 18, [15] = 8,
            [16] = 48, [17] = 9, [18] = 7, [19] = 14, [20] = 11, [21] = 1
        };
        var products = Enumerable.Range(1, 21)
            .Select(id => new PlannedPart(
                id,
                0,
                0,
                recordedSalesById[id],
                new DateTime(2026, 8, 1).AddDays(id)))
            .ToArray();

        var popular = PopularityRanking.ByPopularity(
            products,
            item => PopularityRanking.Score(item.Average, item.Reviews, item.Reviews, item.Sales),
            item => item.Sales,
            item => item.Id).First();
        var latest = PopularityRanking.ByLatest(products, item => item.CreatedAt, item => item.Id).First();

        Assert.Equal(16, popular.Id);
        Assert.Equal(21, latest.Id);
        Assert.NotEqual(popular.Id, latest.Id);
    }

    [Fact]
    public void Explanation_documents_genuine_data_only()
    {
        Assert.Contains("genuine", PopularityRanking.Explanation, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("recorded", PopularityRanking.Explanation, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Demo sales (simulated)", PopularityRanking.Explanation, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Not a verified sale", PopularityDemoPlan.DemoSaleNote);
    }

    [Fact]
    public void Parts_without_customer_ratings_use_the_prior_mean()
    {
        Assert.Equal(PopularityRanking.PriorMean, PopularityRanking.RatingScore(0, 0, 0));
    }

    private static PlannedPart Item(int id, double average, int sales, DateTime createdAt) =>
        new(id, average, 4, sales, createdAt);

    private sealed record PlannedPart(int Id, double Average, int Reviews, int Sales, DateTime CreatedAt);
}
