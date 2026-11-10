using FinalProject.MVC.Repositories;
using FinalProject.MVC.Services.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace FinalProject.MVC.Services;

public sealed class AdminDashboardService : IAdminDashboardService
{
    private readonly IApplicationRepository _db;
    private readonly RoleManager<IdentityRole> _roleManager;

    public AdminDashboardService(IApplicationRepository db, RoleManager<IdentityRole> roleManager)
    {
        _db = db;
        _roleManager = roleManager;
    }

    public async Task<AdminDashboardCounts> GetDashboardCountsAsync(CancellationToken cancellationToken = default)
    {
        var row = await _db.GetAdminDashboardCountsAsync(cancellationToken);
        return new AdminDashboardCounts(
            row.TotalSpareParts,
            row.ActiveSpareParts,
            row.NewContactRequests,
            row.OpenProductQuestions,
            row.RecordedSales,
            row.ActiveCampaigns);
    }

    public async Task<int> CountMembersInRoleAsync(string roleName, CancellationToken cancellationToken = default)
    {
        var role = await _roleManager.FindByNameAsync(roleName);
        if (role == null)
        {
            return 0;
        }

        return await _db.CountUserRolesByRoleIdAsync(role.Id, cancellationToken);
    }

    public async Task<IReadOnlyList<AdminUserActivityRequest>> GetContactRequestsForUserAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var rows = await _db.GetContactRequestActivityForUserAsync(userId, cancellationToken);
        return rows.Select(row => new AdminUserActivityRequest(
            row.Id,
            row.Interest ?? string.Empty,
            row.Status,
            row.CreatedAt)).ToList();
    }
}
