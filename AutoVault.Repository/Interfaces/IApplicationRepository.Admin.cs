namespace FinalProject.MVC.Repositories;

public partial interface IApplicationRepository
{
    Task<AdminDashboardCountsRow> GetAdminDashboardCountsAsync(CancellationToken cancellationToken = default);
    Task<int> CountUserRolesByRoleIdAsync(string roleId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AdminUserActivityRequestRow>> GetContactRequestActivityForUserAsync(
        string userId,
        CancellationToken cancellationToken = default);
}
