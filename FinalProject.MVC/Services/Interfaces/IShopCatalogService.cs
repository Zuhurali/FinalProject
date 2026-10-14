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

    Task<(bool Ok, string? Error)> SaveCampaignAsync(ProductCampaign campaign, CancellationToken cancellationToken = default);

    Task<bool> SetCampaignActiveAsync(int id, bool isActive, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InPersonSale>> GetSalesAsync(CancellationToken cancellationToken = default);

    Task<(bool Ok, string? Error)> RecordSaleAsync(
        int productId,
        int quantity,
        string recordedByUserId,
        string? note,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductInquiry>> GetInquiriesAsync(CancellationToken cancellationToken = default);

    Task<(bool Ok, string? Error)> SaveInquiryAsync(ProductInquiry inquiry, CancellationToken cancellationToken = default);

    Task<bool> SetInquiryHandledAsync(int id, bool handled, CancellationToken cancellationToken = default);
}
