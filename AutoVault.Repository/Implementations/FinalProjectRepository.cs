using FinalProject.Data;
using FinalProject.Repositories;
using Microsoft.EntityFrameworkCore.Storage;

namespace FinalProject.Repositories.Implementations;

/// <summary>
/// Single scoped EF adapter implementing the API aggregate repository interfaces.
/// </summary>
public sealed partial class FinalProjectRepository :
    ICarRepository,
    IProductReviewRepository,
    IProductRatingRepository,
    IMarketplaceRepository,
    INotificationRepository,
    IPartsRepository,
    IShowroomRepository,
    ICatalogAdminRepository
{
    private readonly FinalProjectDbContext _db;

    public FinalProjectRepository(FinalProjectDbContext db)
    {
        _db = db;
    }

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
        _db.Database.BeginTransactionAsync(cancellationToken);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);
}
