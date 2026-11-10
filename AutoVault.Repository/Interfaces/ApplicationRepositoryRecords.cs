namespace FinalProject.MVC.Repositories;

public sealed record ProductPopularityFeedbackRow(
    int ProductId,
    double AverageRating,
    int RatingCount,
    int ReviewCount);

public sealed record ProductPopularitySalesRow(
    int ProductId,
    int RecordedSales);

public sealed record ProductFavoriteCountRow(
    int ProductId,
    int FavoriteCount);

public sealed record AdminDashboardCountsRow(
    int TotalSpareParts,
    int ActiveSpareParts,
    int NewContactRequests,
    int OpenProductQuestions,
    int RecordedSales,
    int ActiveCampaigns);

public sealed record AdminUserActivityRequestRow(
    int Id,
    string? Interest,
    string Status,
    DateTime CreatedAt);

public sealed record CouponUsageCountRow(string Code, int Count);
