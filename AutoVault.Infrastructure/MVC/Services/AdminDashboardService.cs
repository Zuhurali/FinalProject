using FinalProject.MVC.Data;
using FinalProject.MVC.Models;
using FinalProject.MVC.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FinalProject.MVC.Services;

public sealed class AdminDashboardService : IAdminDashboardService
{
    private readonly ApplicationDbContext _db;
    private readonly RoleManager<IdentityRole> _roleManager;

    public AdminDashboardService(ApplicationDbContext db, RoleManager<IdentityRole> roleManager)
    {
        _db = db;
        _roleManager = roleManager;
    }

    public async Task<AdminDashboardCounts> GetDashboardCountsAsync(CancellationToken cancellationToken = default)
    {
        return new AdminDashboardCounts(
            TotalSpareParts: await _db.ShopProducts.CountAsync(cancellationToken),
            ActiveSpareParts: await _db.ShopProducts.CountAsync(product => product.IsActive, cancellationToken),
            NewContactRequests: await _db.ContactRequests.CountAsync(
                request => request.Status == ContactRequestStatus.New,
                cancellationToken),
            OpenProductQuestions: await _db.ProductInquiries.CountAsync(
                inquiry => !inquiry.IsHandled && inquiry.AdminReply == null,
                cancellationToken),
            OpenPurchaseRequests: await _db.ShowroomPurchaseRequests.CountAsync(
                request => request.Status == ShowroomRequestStatus.New
                    || request.Status == ShowroomRequestStatus.Contacted,
                cancellationToken),
            RecordedSales: await _db.InPersonSales.CountAsync(
                sale => !sale.IsDemo && sale.VoidedAt == null,
                cancellationToken),
            ActiveCampaigns: await _db.ProductCampaigns.CountAsync(
                campaign => campaign.IsActive,
                cancellationToken));
    }

    public async Task<int> CountMembersInRoleAsync(string roleName, CancellationToken cancellationToken = default)
    {
        var role = await _roleManager.FindByNameAsync(roleName);
        if (role == null)
        {
            return 0;
        }

        return await _db.UserRoles.CountAsync(userRole => userRole.RoleId == role.Id, cancellationToken);
    }

    public async Task<IReadOnlyList<AdminUserActivityRequest>> GetContactRequestsForUserAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await _db.ContactRequests
            .AsNoTracking()
            .Where(request => request.UserId == userId)
            .OrderByDescending(request => request.CreatedAt)
            .Select(request => new AdminUserActivityRequest(
                request.Id,
                request.Interest,
                request.Status,
                request.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AdminUserActivityRequest>> GetPurchaseRequestsForUserAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await _db.ShowroomPurchaseRequests
            .AsNoTracking()
            .Where(request => request.UserId == userId)
            .OrderByDescending(request => request.CreatedAt)
            .Select(request => new AdminUserActivityRequest(
                request.Id,
                request.Product != null ? request.Product.Name : $"Product #{request.ProductId}",
                request.Status,
                request.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}
