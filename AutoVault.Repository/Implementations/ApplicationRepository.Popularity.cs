using FinalProject.MVC.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FinalProject.MVC.Repositories.Implementations;

public sealed partial class ApplicationRepository
{
    public async Task<IReadOnlyList<ProductPopularityFeedbackRow>> GetProductPopularityFeedbackAsync(
        CancellationToken cancellationToken = default)
    {
        return await _db.Database.SqlQueryRaw<ProductPopularityFeedbackRow>("""
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
    }

    public async Task<IReadOnlyList<ProductPopularitySalesRow>> GetProductPopularitySalesAsync(
        CancellationToken cancellationToken = default)
    {
        return await _db.Database.SqlQueryRaw<ProductPopularitySalesRow>("""
            SELECT
                ProductId,
                CAST(SUM(Quantity) AS int) AS RecordedSales
            FROM InPersonSales
            WHERE IsDemo = 0 AND VoidedAt IS NULL
            GROUP BY ProductId
            """).ToListAsync(cancellationToken);
    }

    public Task<int> DeleteDemoPopularitySalesAsync(
        string demoNote,
        string demoRecorderPrefix,
        CancellationToken cancellationToken = default) =>
        _db.Database.ExecuteSqlInterpolatedAsync($"""
            DELETE FROM InPersonSales
            WHERE IsDemo = 1
              AND (
                    Note = {demoNote}
                 OR RecordedByUserId LIKE {demoRecorderPrefix + "%"}
              );
            """, cancellationToken);
}
