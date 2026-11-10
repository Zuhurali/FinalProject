using FinalProject.MVC.Models;

namespace FinalProject.MVC.Repositories;

public partial interface IShopCatalogRepository
{
    Task<IReadOnlyList<ShopCatalogProduct>> GetActiveShopProductsOrderedAsync(CancellationToken cancellationToken = default);
    Task<ShopCatalogProduct?> GetShopProductByIdAsync(int id, CancellationToken cancellationToken = default);
    void AddShopProduct(ShopCatalogProduct product);
    Task<ShopCatalogProduct?> FindTrackedShopProductAsync(int id, CancellationToken cancellationToken = default);
    Task<int> TryUpdateShopProductQuantityAsync(
        int productId,
        int expectedQuantity,
        int newQuantity,
        ShopCatalogProduct tracked,
        CancellationToken cancellationToken = default);
    void RemoveShopProduct(ShopCatalogProduct product);
    Task<int> CountShopProductDependentsAsync(int productId, CancellationToken cancellationToken = default);
    Task<int> CountShopProductsAsync(CancellationToken cancellationToken = default);
    Task<bool> AnyShopProductsAsync(CancellationToken cancellationToken = default);
    Task SeedShopProductsWithIdentityInsertAsync(IReadOnlyList<ShopCatalogProduct> templates, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductCampaign>> GetAllCampaignsWithProductAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductCampaign>> GetLiveCampaignsAsync(DateTime utcNow, CancellationToken cancellationToken = default);
    Task<ProductCampaign?> GetCampaignByIdWithProductAsync(int id, CancellationToken cancellationToken = default);
    void AddProductCampaign(ProductCampaign campaign);
    Task<ProductCampaign?> FindTrackedProductCampaignAsync(int id, CancellationToken cancellationToken = default);
    void RemoveProductCampaign(ProductCampaign campaign);
    Task<IReadOnlyList<ProductCampaign>> GetOtherActiveCampaignsWithProductAsync(
        int exceptCampaignId,
        CancellationToken cancellationToken = default);
    Task<ShopCatalogProduct?> GetActiveShopProductByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InPersonSale>> GetRecordedSalesWithDetailsAsync(CancellationToken cancellationToken = default);
    Task<InPersonSale?> GetSaleByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default);
    Task<InPersonSale?> FindSaleByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);
    Task<int> DecrementShopProductStockAsync(int productId, int quantity, CancellationToken cancellationToken = default);
    void AddInPersonSale(InPersonSale sale);
    Task<int> ClaimVoidInPersonSaleAsync(int saleId, DateTime voidedAt, string voidedBy, CancellationToken cancellationToken = default);
    Task<InPersonSale> GetSaleByIdRequiredAsync(int saleId, CancellationToken cancellationToken = default);
    Task<int> IncrementShopProductStockAsync(int productId, int quantity, CancellationToken cancellationToken = default);
    Task ReopenShowroomRequestAfterVoidAsync(int requestId, DateTime updatedAt, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductInquiry>> GetProductInquiriesAsync(string? status, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductInquiry>> GetAnsweredProductInquiriesAsync(int productId, CancellationToken cancellationToken = default);
    Task<ProductInquiry?> GetProductInquiryByIdWithProductAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductInquiry>> GetProductInquiriesForUserAsync(string userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductInquiry>> FindOrphanedProductInquiriesByEmailAsync(string targetEmail, CancellationToken cancellationToken = default);
    Task<bool> AnyActiveShopProductAsync(int productId, CancellationToken cancellationToken = default);
    void AddProductInquiry(ProductInquiry inquiry);
    Task<ProductInquiry?> FindTrackedProductInquiryAsync(int id, CancellationToken cancellationToken = default);
    void RemoveProductInquiry(ProductInquiry inquiry);
    Task TryInsertInquiryReplyNotificationAsync(
        string userId,
        int inquiryId,
        string dedupKey,
        string body,
        DateTime createdAt,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ShowroomPurchaseRequest>> GetShowroomPurchaseRequestsAsync(
        string? status,
        CancellationToken cancellationToken = default);
    Task<ShowroomPurchaseRequest?> GetShowroomPurchaseRequestByIdWithProductAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ShowroomPurchaseRequest>> GetShowroomPurchaseRequestsForUserAsync(
        string userId,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ShowroomPurchaseRequest>> FindOrphanedShowroomPurchaseRequestsAsync(CancellationToken cancellationToken = default);
    Task<ShowroomPurchaseRequest?> FindShowroomPurchaseRequestBySubmissionKeyAsync(
        Guid submissionKey,
        CancellationToken cancellationToken = default);
    Task<ShowroomPurchaseRequest?> FindOpenShowroomPurchaseRequestAsync(
        int productId,
        string email,
        CancellationToken cancellationToken = default);
    Task<int> SumOpenReservedQuantityAsync(int productId, int? excludeRequestId, CancellationToken cancellationToken = default);
    void AddShowroomPurchaseRequest(ShowroomPurchaseRequest request);
    Task<ShowroomPurchaseRequest?> FindTrackedShowroomPurchaseRequestAsync(int id, CancellationToken cancellationToken = default);
    void RemoveShowroomPurchaseRequest(ShowroomPurchaseRequest request);
    Task<int> ClaimShowroomPurchaseRequestForCompletionAsync(
        int requestId,
        DateTime completedAt,
        CancellationToken cancellationToken = default);
    Task<ShowroomPurchaseRequest> GetShowroomPurchaseRequestByIdRequiredAsync(
        int requestId,
        CancellationToken cancellationToken = default);
}
