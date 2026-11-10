using FinalProject.Models.Entities;

namespace FinalProject.Repositories;

public sealed record CarListQueryCriteria(
    string? Search,
    int? BrandId,
    int? CategoryId,
    string? Model,
    decimal? MinPrice,
    decimal? MaxPrice,
    int? MinYear,
    int? MaxYear,
    int? MinMileage,
    int? MaxMileage,
    IReadOnlyList<int> SelectedSeats,
    string? Fuel,
    string? Transmission,
    bool? IsNew,
    string? Status,
    string? Sort,
    bool IncludeListingText);

public sealed record CarBrandModelRow(int BrandId, string Model);

public sealed record CarIdCountRow(int CarId, int Count);

public sealed record CarIdCreatedAtRow(int Id, DateTime CreatedAt);

public interface ICarRepository
{
    Task<int> CountCarsMatchingAsync(CarListQueryCriteria criteria, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Car>> GetCarsMatchingPageAsync(
        CarListQueryCriteria criteria,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetBrandNamesContainingAsync(
        string termLower,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetActiveCarDescriptionsMatchingAsync(
        string activeStatus,
        string termLower,
        CancellationToken cancellationToken = default);

    Task<Car?> GetCarByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Car>> GetOwnedCarsWithDetailsAsync(
        string ownerId,
        CancellationToken cancellationToken = default);

    Task<bool> UserExistsAsync(string userId, CancellationToken cancellationToken = default);

    Task<bool> LookupsExistAsync(int brandId, int categoryId, CancellationToken cancellationToken = default);

    Task<Brand?> GetBrandByIdAsync(int brandId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<int>> GetDistinctBrandIdsForModelAsync(
        string modelLower,
        CancellationToken cancellationToken = default);

    Task<Car> AddCarAsync(Car car, CancellationToken cancellationToken = default);

    Task<Car?> FindCarForUpdateAsync(int id, CancellationToken cancellationToken = default);

    Task<Car?> FindCarByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<bool> DeleteCarAsync(int id, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    void RemoveCarImage(CarImage image);

    Task<IReadOnlyList<string>> GetImageUrlsUsedByOtherCarsAsync(
        int excludeCarId,
        IReadOnlyCollection<string> candidateUrls,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Brand>> GetBrandsOrderedAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Category>> GetCategoriesOrderedAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CarBrandModelRow>> GetNonEmptyCarModelsByBrandAsync(
        CancellationToken cancellationToken = default);

    Task<bool> ActiveCarExistsAsync(
        int carId,
        string activeStatus,
        CancellationToken cancellationToken = default);

    Task AddCarViewAsync(CarView view, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CarIdCountRow>> GetCarViewCountsAsync(
        DateTime? viewedAtFromUtc,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CarIdCountRow>> GetFavoriteCountsByCarAsync(
        CancellationToken cancellationToken = default);

    Task<int> CountActiveCarsAsync(string activeStatus, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Car>> GetLatestActiveCarsPageAsync(
        string activeStatus,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<int> CountCarViewsAsync(CancellationToken cancellationToken = default);

    Task<int> CountCarViewsSinceAsync(DateTime viewedAtFromUtc, CancellationToken cancellationToken = default);

    Task<int> CountFavoritesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DateTime>> GetCarViewTimesSinceAsync(
        DateTime viewedAtFromUtc,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CarIdCreatedAtRow>> GetActiveUsedCarSummariesAsync(
        string activeStatus,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CarIdCountRow>> GetCarViewCountsForCarsAsync(
        IReadOnlyCollection<int> carIds,
        DateTime? viewedAtFromUtc,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<int, DateTime>> GetActiveCarCreatedAtByIdsAsync(
        string activeStatus,
        IReadOnlyCollection<int> carIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Car>> GetCarsWithDetailsByIdsAsync(
        IReadOnlyCollection<int> carIds,
        CancellationToken cancellationToken = default);
}
