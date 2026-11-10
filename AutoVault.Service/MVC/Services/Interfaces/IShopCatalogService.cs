using FinalProject.MVC.Models;

namespace FinalProject.MVC.Services.Interfaces;

public interface IShopCatalogService
{
    Task<IReadOnlyList<ShopProduct>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<ShopCatalogProduct?> GetRecordAsync(int id, CancellationToken cancellationToken = default);

    Task<ShopCatalogProduct> CreateAsync(ShopCatalogProduct product, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(ShopCatalogProduct product, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);

    Task<int> CountAsync(CancellationToken cancellationToken = default);

    Task EnsureSeedAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductCampaign>> GetCampaignsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductCampaign>> GetLiveCampaignsAsync(CancellationToken cancellationToken = default);

    Task<ProductCampaign?> GetCampaignAsync(int id, CancellationToken cancellationToken = default);

    Task<(bool Ok, string? Error)> SaveCampaignAsync(ProductCampaign campaign, CancellationToken cancellationToken = default);

    Task<(bool Ok, string? Error)> SetCampaignActiveAsync(int id, bool isActive, CancellationToken cancellationToken = default);

    Task<(bool Ok, string? Error)> DeleteCampaignAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InPersonSale>> GetSalesAsync(CancellationToken cancellationToken = default);

    Task<InPersonSale?> GetSaleAsync(int id, CancellationToken cancellationToken = default);

    /// <param name="couponCode">Optional showroom courtesy code; redeemed in SQL when the sale succeeds.</param>
    /// <param name="customerUserId">Required with <paramref name="couponCode"/> — Identity user receiving the redemption.</param>
    Task<(bool Ok, string? Error)> RecordSaleAsync(
        int productId,
        int quantity,
        string recordedByUserId,
        string? note,
        Guid? idempotencyKey = null,
        string? couponCode = null,
        string? customerUserId = null,
        CancellationToken cancellationToken = default);

    Task<(bool Ok, string? Error)> VoidSaleAsync(
        int saleId,
        string voidedByUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductInquiry>> GetInquiriesAsync(
        string? status = null,
        CancellationToken cancellationToken = default);

    /// <summary>Public product page: answered Q&amp;A for one product only (no admin inbox dump).</summary>
    Task<IReadOnlyList<ProductInquiry>> GetAnsweredInquiriesForProductAsync(
        int productId,
        CancellationToken cancellationToken = default);

    Task<ProductInquiry?> GetInquiryAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductInquiry>> GetInquiriesForUserAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<int> ClaimOrphanedInquiriesByEmailAsync(
        string userId,
        string accountEmail,
        CancellationToken cancellationToken = default);

    Task<(bool Ok, string? Error)> SaveInquiryAsync(ProductInquiry inquiry, CancellationToken cancellationToken = default);

    Task<bool> SetInquiryHandledAsync(int id, bool handled, CancellationToken cancellationToken = default);

    Task<(bool Ok, string? Error)> SaveInquiryReplyAsync(
        int id,
        string reply,
        string adminUserId,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteInquiryAsync(int id, CancellationToken cancellationToken = default);
}
