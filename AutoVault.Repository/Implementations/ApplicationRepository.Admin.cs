using FinalProject.MVC.Models;
using FinalProject.MVC.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FinalProject.MVC.Repositories.Implementations;

public sealed partial class ApplicationRepository
{
    public async Task<AdminDashboardCountsRow> GetAdminDashboardCountsAsync(CancellationToken cancellationToken = default)
    {
        return new AdminDashboardCountsRow(
            TotalSpareParts: await _db.ShopProducts.CountAsync(cancellationToken),
            ActiveSpareParts: await _db.ShopProducts.CountAsync(product => product.IsActive, cancellationToken),
            NewContactRequests: await _db.ContactRequests.CountAsync(
                request => request.Status == ContactRequestStatus.New,
                cancellationToken),
            OpenProductQuestions: await _db.ProductInquiries.CountAsync(
                inquiry => !inquiry.IsHandled && inquiry.AdminReply == null,
                cancellationToken),
            RecordedSales: await _db.InPersonSales.CountAsync(
                sale => !sale.IsDemo && sale.VoidedAt == null,
                cancellationToken),
            ActiveCampaigns: await _db.ProductCampaigns.CountAsync(
                campaign => campaign.IsActive,
                cancellationToken));
    }

    public Task<int> CountUserRolesByRoleIdAsync(string roleId, CancellationToken cancellationToken = default) =>
        _db.UserRoles.CountAsync(userRole => userRole.RoleId == roleId, cancellationToken);

    public async Task<IReadOnlyList<AdminUserActivityRequestRow>> GetContactRequestActivityForUserAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await _db.ContactRequests
            .AsNoTracking()
            .Where(request => request.UserId == userId)
            .OrderByDescending(request => request.CreatedAt)
            .Select(request => new AdminUserActivityRequestRow(
                request.Id,
                request.Interest,
                request.Status,
                request.CreatedAt))
            .ToListAsync(cancellationToken);
    }

}
