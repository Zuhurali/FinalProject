using FinalProject.MVC.Models;
using FinalProject.MVC.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FinalProject.MVC.Repositories.Implementations;

public sealed partial class ApplicationRepository
{
    public async Task<IReadOnlyList<Coupon>> GetAllCouponsWithProductAsync(CancellationToken cancellationToken = default) =>
        await _db.Coupons.AsNoTracking()
            .Include(coupon => coupon.Product)
            .OrderByDescending(coupon => coupon.IsActive)
            .ThenBy(coupon => coupon.Code)
            .ToListAsync(cancellationToken);

    public Task<Coupon?> GetCouponByIdWithProductAsync(int id, CancellationToken cancellationToken = default) =>
        _db.Coupons.AsNoTracking()
            .Include(coupon => coupon.Product)
            .FirstOrDefaultAsync(coupon => coupon.Id == id, cancellationToken);

    public Task<Coupon?> GetCouponByCodeAsync(string normalizedCode, CancellationToken cancellationToken = default) =>
        _db.Coupons.AsNoTracking()
            .FirstOrDefaultAsync(coupon => coupon.Code == normalizedCode, cancellationToken);

    public Task<bool> IsActiveShopProductAsync(int productId, CancellationToken cancellationToken = default) =>
        _db.ShopProducts.AsNoTracking()
            .AnyAsync(product => product.Id == productId && product.IsActive, cancellationToken);

    public Task<bool> AnyCouponWithCodeExceptIdAsync(string code, int exceptId, CancellationToken cancellationToken = default) =>
        _db.Coupons.AsNoTracking()
            .AnyAsync(item => item.Code == code && item.Id != exceptId, cancellationToken);

    public void AddCoupon(Coupon coupon) => _db.Coupons.Add(coupon);

    public Task<Coupon?> FindTrackedCouponAsync(int id, CancellationToken cancellationToken = default) =>
        _db.Coupons.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

    public Task<int> CountCouponUsagesByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        _db.CouponUsages.CountAsync(item => item.CouponCode == code, cancellationToken);

    public Task<int> CountCouponUsagesByUserAndCodeAsync(
        string userId,
        string code,
        CancellationToken cancellationToken = default) =>
        _db.CouponUsages.CountAsync(item => item.UserId == userId && item.CouponCode == code, cancellationToken);

    public void RemoveCoupon(Coupon coupon) => _db.Coupons.Remove(coupon);

    public void AddCouponUsage(CouponUsage usage) => _db.CouponUsages.Add(usage);

    public Task<bool> AnyCouponWithCodeAsync(string code, CancellationToken cancellationToken = default) =>
        _db.Coupons.AnyAsync(coupon => coupon.Code == code, cancellationToken);

    public async Task<IReadOnlyList<CouponUsageCountRow>> GetCouponUsageCountsByCodeAsync(
        CancellationToken cancellationToken = default)
    {
        var rows = await _db.CouponUsages.AsNoTracking()
            .GroupBy(usage => usage.CouponCode)
            .Select(group => new CouponUsageCountRow(group.Key, group.Count()))
            .ToListAsync(cancellationToken);
        return rows;
    }

    public async Task<IReadOnlyList<CouponUsage>> GetRecentCouponUsagesAsync(
        int take,
        CancellationToken cancellationToken = default)
    {
        var limit = take < 1 ? 50 : Math.Min(take, 200);
        return await _db.CouponUsages.AsNoTracking()
            .Include(usage => usage.User)
            .OrderByDescending(usage => usage.UsedAt)
            .ThenByDescending(usage => usage.Id)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }
}
