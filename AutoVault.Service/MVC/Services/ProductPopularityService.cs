using FinalProject.MVC.Commerce;
using FinalProject.MVC.Models;
using FinalProject.MVC.Repositories;
using FinalProject.MVC.Services.Interfaces;

namespace FinalProject.MVC.Services;

public sealed class ProductPopularityService : IProductPopularityService
{
    private readonly IApplicationRepository _db;

    public ProductPopularityService(IApplicationRepository db)
    {
        _db = db;
    }

    public async Task ApplyAsync(IList<ShopProduct> products, CancellationToken cancellationToken = default)
    {
        if (products.Count == 0)
        {
            return;
        }

        var feedback = await _db.GetProductPopularityFeedbackAsync(cancellationToken);
        var sales = await _db.GetProductPopularitySalesAsync(cancellationToken);

        var feedbackByProduct = feedback.ToDictionary(row => row.ProductId);
        var salesByProduct = sales.ToDictionary(row => row.ProductId);
        foreach (var product in products)
        {
            feedbackByProduct.TryGetValue(product.Id, out var opinion);
            salesByProduct.TryGetValue(product.Id, out var sold);
            product.Rating = opinion?.AverageRating ?? 0;
            product.RatingCount = opinion?.RatingCount ?? 0;
            product.ReviewCount = opinion?.ReviewCount ?? 0;
            product.RealSalesCount = sold?.RecordedSales ?? 0;
            product.DemoSalesCount = 0;
            product.PopularityScore = PopularityRanking.Score(
                product.Rating,
                product.RatingCount,
                product.ReviewCount,
                product.RealSalesCount);
        }
    }
}
