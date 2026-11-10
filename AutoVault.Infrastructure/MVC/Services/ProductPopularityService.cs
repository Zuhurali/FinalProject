using FinalProject.MVC.Commerce;
using FinalProject.MVC.Data;
using FinalProject.MVC.Models;
using FinalProject.MVC.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinalProject.MVC.Services;

public sealed class ProductPopularityService : IProductPopularityService
{
    private readonly ApplicationDbContext _db;

    public ProductPopularityService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task ApplyAsync(IList<ShopProduct> products, CancellationToken cancellationToken = default)
    {
        if (products.Count == 0)
        {
            return;
        }

        var feedback = await _db.Database.SqlQueryRaw<ProductFeedbackRow>("""
            SELECT
                p.Id AS ProductId,
                CAST(ISNULL(AVG(CAST(r.Rating AS float)), 0) AS float) AS AverageRating,
                CAST(COUNT(r.Rating) AS int) AS RatingCount,
                CAST(ISNULL((
                    SELECT COUNT(*)
                    FROM ProductReviews AS rv
                    WHERE rv.ProductId = p.Id AND rv.IsHidden = 0 AND rv.IsDemo = 0
                ), 0) AS int) AS ReviewCount
            FROM ShopProducts AS p
            LEFT JOIN ProductRatings AS r ON r.ProductId = p.Id AND r.IsDemo = 0
            GROUP BY p.Id
            """).ToListAsync(cancellationToken);
        var sales = await _db.Database.SqlQueryRaw<ProductSalesRow>("""
            SELECT
                ProductId,
                CAST(SUM(Quantity) AS int) AS RecordedSales
            FROM InPersonSales
            WHERE IsDemo = 0 AND VoidedAt IS NULL
            GROUP BY ProductId
            """).ToListAsync(cancellationToken);

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

public sealed class ProductFeedbackRow
{
    public int ProductId { get; set; }
    public double AverageRating { get; set; }
    public int RatingCount { get; set; }
    public int ReviewCount { get; set; }
}

public sealed class ProductSalesRow
{
    public int ProductId { get; set; }
    public int RecordedSales { get; set; }
}
