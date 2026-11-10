namespace FinalProject.MVC.Services.Interfaces;

public interface IAdminDashboardService
{
    Task<AdminDashboardCounts> GetDashboardCountsAsync(CancellationToken cancellationToken = default);

    Task<int> CountMembersInRoleAsync(string roleName, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdminUserActivityRequest>> GetContactRequestsForUserAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdminUserActivityRequest>> GetPurchaseRequestsForUserAsync(
        string userId,
        CancellationToken cancellationToken = default);
}

public sealed record AdminDashboardCounts(
    int TotalSpareParts,
    int ActiveSpareParts,
    int NewContactRequests,
    int OpenProductQuestions,
    int OpenPurchaseRequests,
    int RecordedSales,
    int ActiveCampaigns);

public sealed record AdminUserActivityRequest(
    int Id,
    string Summary,
    string Status,
    DateTime CreatedAt);
