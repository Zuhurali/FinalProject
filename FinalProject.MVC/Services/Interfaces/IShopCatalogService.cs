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

    Task<ProductCampaign?> GetCampaignAsync(int id, CancellationToken cancellationToken = default);

    Task<(bool Ok, string? Error)> SaveCampaignAsync(ProductCampaign campaign, CancellationToken cancellationToken = default);

    Task<(bool Ok, string? Error)> SetCampaignActiveAsync(int id, bool isActive, CancellationToken cancellationToken = default);

    Task<bool> DeleteCampaignAsync(int id, CancellationToken cancellationToken = default);

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

    Task<(bool Ok, string? Error)> SaveInquiryReplyAsync(
        int id,
        string reply,
        string adminUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ShowroomPurchaseRequest>> GetPurchaseRequestsAsync(CancellationToken cancellationToken = default);

    Task<PurchaseRequestResult> SavePurchaseRequestAsync(ShowroomPurchaseRequest request, CancellationToken cancellationToken = default);

    Task<bool> SetPurchaseRequestStatusAsync(int id, string status, CancellationToken cancellationToken = default);
}

public sealed record PurchaseRequestResult(bool Ok, bool Duplicate, int? RequestId, string? Error)
{
    public static PurchaseRequestResult Created(int id) => new(true, false, id, null);

    public static PurchaseRequestResult AlreadyReceived(int id) => new(true, true, id, null);

    public static PurchaseRequestResult Failed(string error) => new(false, false, null, error);
}
