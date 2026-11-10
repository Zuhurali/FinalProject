using FinalProject.MVC.Models;

namespace FinalProject.MVC.Repositories;

public partial interface IApplicationRepository
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    /// <summary>Saves changes; on concurrency/unique conflict clears the tracker and returns false.</summary>
    Task<bool> TrySaveChangesAsync(CancellationToken cancellationToken = default);
    void ClearChangeTracker();
    Task<IApplicationRepositoryTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}

public partial interface IShopCatalogRepository : IApplicationRepository;
public partial interface IOrderRepository : IApplicationRepository;
public partial interface ICouponRepository : IApplicationRepository;
public partial interface IContactRequestRepository : IApplicationRepository;
