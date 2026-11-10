using FinalProject.MVC.Models;

namespace FinalProject.MVC.Repositories;

public partial interface IApplicationRepository
{
    Task<IReadOnlyList<Coupon>> GetAllCouponsWithProductAsync(CancellationToken cancellationToken = default);
    Task<Coupon?> GetCouponByIdWithProductAsync(int id, CancellationToken cancellationToken = default);
    Task<Coupon?> GetCouponByCodeAsync(string normalizedCode, CancellationToken cancellationToken = default);
    Task<bool> IsActiveShopProductAsync(int productId, CancellationToken cancellationToken = default);
    Task<bool> AnyCouponWithCodeExceptIdAsync(string code, int exceptId, CancellationToken cancellationToken = default);
    void AddCoupon(Coupon coupon);
    Task<Coupon?> FindTrackedCouponAsync(int id, CancellationToken cancellationToken = default);
    Task<int> CountCouponUsagesByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<int> CountCouponUsagesByUserAndCodeAsync(string userId, string code, CancellationToken cancellationToken = default);
    void RemoveCoupon(Coupon coupon);
    void AddCouponUsage(CouponUsage usage);
    Task<bool> AnyCouponWithCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CouponUsageCountRow>> GetCouponUsageCountsByCodeAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CouponUsage>> GetRecentCouponUsagesAsync(int take, CancellationToken cancellationToken = default);
}
