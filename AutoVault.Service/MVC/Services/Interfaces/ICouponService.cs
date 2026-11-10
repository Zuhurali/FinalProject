using FinalProject.MVC.Models;

namespace FinalProject.MVC.Services.Interfaces;

public interface ICouponService
{
    Task<IReadOnlyList<Coupon>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Coupon?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Coupon?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    Task<(bool Ok, string? Error)> SaveAsync(Coupon coupon, CancellationToken cancellationToken = default);

    Task<bool> SetActiveAsync(int id, bool isActive, CancellationToken cancellationToken = default);

    Task<(bool Ok, string? Error)> DeleteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a coupon against the cart and returns the server-calculated discount.
    /// Does not write CouponUsages (preview / session apply only).
    /// </summary>
    Task<(bool Ok, string Message, decimal DiscountAmount, string? NormalizedCode)> PreviewApplyAsync(
        string? code,
        string userId,
        CartViewModel cart,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists a redemption after a successful in-person sale (or equivalent store workflow).
    /// Not used for online checkout — AutoVault does not take online payment.
    /// Idempotent for the same user+code under the unique usage index.
    /// </summary>
    Task<(bool Ok, string? Error)> RedeemAsync(
        string userId,
        string? code,
        decimal discountAmount,
        CancellationToken cancellationToken = default);

    Task EnsureSeedAsync(CancellationToken cancellationToken = default);

    /// <summary>Redemption counts from CouponUsages keyed by normalized coupon code.</summary>
    Task<IReadOnlyDictionary<string, int>> GetUsageCountsByCodeAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CouponUsage>> GetRecentUsagesAsync(int take = 50, CancellationToken cancellationToken = default);
}
