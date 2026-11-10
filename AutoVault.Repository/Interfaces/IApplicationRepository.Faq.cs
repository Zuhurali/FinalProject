using FinalProject.MVC.Models;

namespace FinalProject.MVC.Repositories;

public partial interface IApplicationRepository
{
    Task<bool> AnyPublishedShopFaqItemsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FaqItem>> GetPublishedFaqItemsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FaqItem>> GetShopPublishedFaqItemsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FaqItem>> GetAllFaqItemsAsync(CancellationToken cancellationToken = default);
    Task<FaqItem?> GetFaqItemByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<FaqItem?> FindTrackedFaqItemAsync(int id, CancellationToken cancellationToken = default);
    Task<int?> GetMaxFaqSortOrderAsync(CancellationToken cancellationToken = default);
    void AddFaqItem(FaqItem item);
    void RemoveFaqItem(FaqItem item);
    Task<IReadOnlyList<FaqItem>> GetFaqItemsOrderedForMoveAsync(CancellationToken cancellationToken = default);
}
