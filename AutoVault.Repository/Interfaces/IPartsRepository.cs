using FinalProject.Models.Entities;

namespace FinalProject.Repositories;

public sealed record PartRatingAggregateRow(int ProductId, double Average, int Count);

public sealed record PartAdminListPage(
    IReadOnlyList<ShopProductReference> Items,
    IReadOnlyList<string> Categories,
    IReadOnlyList<string> Brands,
    int Total,
    int Page,
    int PageSize);

public interface IPartsRepository
{
    Task<PartAdminListPage> ListPartsAsync(
        string? search,
        string? category,
        string? brand,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<ShopProductReference?> GetPartByIdNoTrackingAsync(int id, CancellationToken cancellationToken = default);
    Task<ShopProductReference?> GetPartByIdTrackedAsync(int id, CancellationToken cancellationToken = default);

    Task<(ShopProductReference? Part, string? Error)> AddPartAsync(
        ShopProductReference part,
        CancellationToken cancellationToken = default);

    Task<(bool StockConcurrencyFailed, string? SaveError)> SavePartUpdateAsync(
        ShopProductReference part,
        int expectedQuantity,
        int newQuantity,
        bool quantityChanged,
        CancellationToken cancellationToken = default);

    Task<(bool Found, string Outcome, string Message)> DeletePartAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PartRatingAggregateRow>> GetRatingAggregatesAsync(
        IReadOnlyCollection<int> productIds,
        CancellationToken cancellationToken = default);
}
