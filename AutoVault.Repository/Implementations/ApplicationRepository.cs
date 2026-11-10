using FinalProject.MVC.Data;
using FinalProject.MVC.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FinalProject.MVC.Repositories.Implementations;

public sealed partial class ApplicationRepository :
    IApplicationRepository,
    IShopCatalogRepository,
    IOrderRepository,
    ICouponRepository,
    IContactRequestRepository
{
    private readonly ApplicationDbContext _db;

    public ApplicationRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);

    public async Task<bool> TrySaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            _db.ChangeTracker.Clear();
            return false;
        }
    }

    public void ClearChangeTracker() => _db.ChangeTracker.Clear();

    public async Task<IApplicationRepositoryTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        return new ApplicationRepositoryTransaction(transaction);
    }
}
