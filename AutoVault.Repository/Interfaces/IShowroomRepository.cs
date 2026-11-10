using FinalProject.Models.Entities;

namespace FinalProject.Repositories;

public sealed record ShowroomSalePriceRow(
    decimal UnitPrice,
    decimal ListPrice,
    int? CampaignId,
    decimal? CampaignPercent);

public sealed record ShowroomRecordSaleResult(
    bool Success,
    string? Error,
    InPersonSaleReference? Sale,
    ShopProductReference? Product);

public sealed record ShowroomCompleteWithSaleResult(
    bool Success,
    string? Error,
    InPersonSaleReference? Sale,
    ShopProductReference? Product,
    string? ExistingRequestStatus,
    bool RequestMissing);

public sealed record ShowroomVoidSaleResult(bool Success, string? Error);

public interface IShowroomRepository
{
    Task<IReadOnlyList<ShowroomPurchaseRequestReference>> ListPurchaseRequestsAsync(
        string? status,
        CancellationToken cancellationToken = default);

    Task<ShowroomPurchaseRequestReference?> GetPurchaseRequestWithProductAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<ShowroomPurchaseRequestReference?> GetPurchaseRequestTrackedAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<bool> DeletePurchaseRequestAsync(int id, CancellationToken cancellationToken = default);

    Task<InPersonSaleReference?> FindActiveSaleByIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<ShopProductReference?> GetProductByIdAsync(int productId, CancellationToken cancellationToken = default);

    Task<int> SumOpenReservedQuantityAsync(
        int productId,
        int? excludeRequestId,
        CancellationToken cancellationToken = default);

    Task<ShowroomSalePriceRow> ResolveSalePriceAsync(
        ShopProductReference product,
        CancellationToken cancellationToken = default);

    Task<ShowroomRecordSaleResult> RecordInPersonSaleAsync(
        int productId,
        int quantity,
        string recordedByUserId,
        string note,
        string? idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<ShowroomCompleteWithSaleResult> CompletePurchaseRequestWithSaleAsync(
        int requestId,
        string recordedByUserId,
        string? note,
        string completeIdempotencyKey,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InPersonSaleReference>> ListRecentSalesAsync(
        CancellationToken cancellationToken = default);

    Task<ShowroomVoidSaleResult> VoidSaleAsync(
        int saleId,
        string voidedByUserId,
        CancellationToken cancellationToken = default);

    Task<ShowroomPurchaseRequestReference?> GetPurchaseRequestByIdAsync(
        int id,
        CancellationToken cancellationToken = default);
}
