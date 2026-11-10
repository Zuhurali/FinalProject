using FinalProject.Models.Entities;
using FinalProject.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FinalProject.Repositories.Implementations;

public sealed partial class FinalProjectRepository
{
    private IQueryable<Car> CarsAsNoTracking() => _db.Cars.AsNoTracking();

    private static IQueryable<Car> WithListIncludes(IQueryable<Car> cars) =>
        cars
            .Include(car => car.Brand)
            .Include(car => car.Category)
            .Include(car => car.Owner)
            .Include(car => car.Images)
            .AsSplitQuery();

    public Task<int> CountCarsMatchingAsync(CarListQueryCriteria criteria, CancellationToken cancellationToken = default) =>
        ApplyFilters(CarsAsNoTracking(), criteria).CountAsync(cancellationToken);

    public async Task<IReadOnlyList<Car>> GetCarsMatchingPageAsync(
        CarListQueryCriteria criteria,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var filtered = ApplyFilters(CarsAsNoTracking(), criteria);
        return await WithListIncludes(ApplySort(filtered, criteria.Sort))
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public Task<IReadOnlyList<string>> GetBrandNamesContainingAsync(
        string termLower,
        CancellationToken cancellationToken = default) =>
        _db.Brands.AsNoTracking()
            .Where(brand => brand.Name.ToLower().Contains(termLower))
            .Select(brand => brand.Name)
            .Distinct()
            .ToListAsync(cancellationToken)
            .ContinueWith(task => (IReadOnlyList<string>)task.Result, cancellationToken);

    public Task<IReadOnlyList<string>> GetActiveCarDescriptionsMatchingAsync(
        string activeStatus,
        string termLower,
        CancellationToken cancellationToken = default)
    {
        var statusLower = activeStatus.ToLower();
        return _db.Cars.AsNoTracking()
            .Where(car =>
                car.Status.ToLower() == statusLower
                && (car.Description.ToLower().Contains(termLower)
                    || car.Model.ToLower().Contains(termLower)
                    || car.Brand.Name.ToLower().Contains(termLower)))
            .Select(car => car.Description)
            .Where(name => name != "")
            .Distinct()
            .ToListAsync(cancellationToken)
            .ContinueWith(task => (IReadOnlyList<string>)task.Result, cancellationToken);
    }

    public Task<Car?> GetCarByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default) =>
        WithListIncludes(CarsAsNoTracking())
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

    public Task<IReadOnlyList<Car>> GetOwnedCarsWithDetailsAsync(
        string ownerId,
        CancellationToken cancellationToken = default) =>
        WithListIncludes(CarsAsNoTracking().Where(car => car.OwnerId == ownerId)
            .OrderByDescending(car => car.CreatedAt))
            .ToListAsync(cancellationToken)
            .ContinueWith(task => (IReadOnlyList<Car>)task.Result, cancellationToken);

    public async Task<bool> LookupsExistAsync(int brandId, int categoryId, CancellationToken cancellationToken = default)
    {
        return await _db.Brands.AsNoTracking().AnyAsync(brand => brand.Id == brandId, cancellationToken)
            && await _db.Categories.AsNoTracking().AnyAsync(category => category.Id == categoryId, cancellationToken);
    }

    public Task<Brand?> GetBrandByIdAsync(int brandId, CancellationToken cancellationToken = default) =>
        _db.Brands.AsNoTracking().FirstOrDefaultAsync(item => item.Id == brandId, cancellationToken);

    public Task<IReadOnlyList<int>> GetDistinctBrandIdsForModelAsync(
        string modelLower,
        CancellationToken cancellationToken = default) =>
        _db.Cars.AsNoTracking()
            .Where(car => car.Model.ToLower() == modelLower)
            .Select(car => car.BrandId)
            .Distinct()
            .ToListAsync(cancellationToken)
            .ContinueWith(task => (IReadOnlyList<int>)task.Result, cancellationToken);

    public async Task<Car> AddCarAsync(Car car, CancellationToken cancellationToken = default)
    {
        _db.Cars.Add(car);
        await _db.SaveChangesAsync(cancellationToken);
        return car;
    }

    public Task<Car?> FindCarForUpdateAsync(int id, CancellationToken cancellationToken = default) =>
        _db.Cars
            .Include(item => item.Images)
            .Include(item => item.Brand)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

    public async Task<Car?> FindCarByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await _db.Cars.FindAsync([id], cancellationToken);

    public async Task<bool> DeleteCarAsync(int id, CancellationToken cancellationToken = default)
    {
        var car = await _db.Cars.FindAsync([id], cancellationToken);
        if (car == null)
        {
            return false;
        }

        _db.Cars.Remove(car);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    Task ICarRepository.SaveChangesAsync(CancellationToken cancellationToken) =>
        _db.SaveChangesAsync(cancellationToken);

    public void RemoveCarImage(CarImage image) => _db.Remove(image);

    public Task<IReadOnlyList<string>> GetImageUrlsUsedByOtherCarsAsync(
        int excludeCarId,
        IReadOnlyCollection<string> candidateUrls,
        CancellationToken cancellationToken = default) =>
        _db.CarImages.AsNoTracking()
            .Where(image => image.CarId != excludeCarId && candidateUrls.Contains(image.ImageUrl))
            .Select(image => image.ImageUrl)
            .ToListAsync(cancellationToken)
            .ContinueWith(task => (IReadOnlyList<string>)task.Result, cancellationToken);

    public Task<IReadOnlyList<Brand>> GetBrandsOrderedAsync(CancellationToken cancellationToken = default) =>
        _db.Brands.AsNoTracking()
            .OrderBy(brand => brand.Name)
            .ToListAsync(cancellationToken)
            .ContinueWith(task => (IReadOnlyList<Brand>)task.Result, cancellationToken);

    public Task<IReadOnlyList<Category>> GetCategoriesOrderedAsync(CancellationToken cancellationToken = default) =>
        _db.Categories.AsNoTracking()
            .OrderBy(category => category.Name)
            .ToListAsync(cancellationToken)
            .ContinueWith(task => (IReadOnlyList<Category>)task.Result, cancellationToken);

    public async Task<IReadOnlyList<CarBrandModelRow>> GetNonEmptyCarModelsByBrandAsync(
        CancellationToken cancellationToken = default)
    {
        var rows = await _db.Cars.AsNoTracking()
            .Where(car => car.Model != "")
            .Select(car => new CarBrandModelRow(car.BrandId, car.Model))
            .ToListAsync(cancellationToken);
        return rows;
    }

    public Task<bool> ActiveCarExistsAsync(
        int carId,
        string activeStatus,
        CancellationToken cancellationToken = default)
    {
        var statusLower = activeStatus.ToLower();
        return _db.Cars.AsNoTracking()
            .AnyAsync(
                car => car.Id == carId && car.Status.ToLower() == statusLower,
                cancellationToken);
    }

    public async Task AddCarViewAsync(CarView view, CancellationToken cancellationToken = default)
    {
        _db.CarViews.Add(view);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CarIdCountRow>> GetCarViewCountsAsync(
        DateTime? viewedAtFromUtc,
        CancellationToken cancellationToken = default)
    {
        var views = _db.CarViews.AsNoTracking();
        if (viewedAtFromUtc != null)
        {
            views = views.Where(view => view.ViewedAt >= viewedAtFromUtc.Value);
        }

        return await views
            .GroupBy(view => view.CarId)
            .Select(group => new CarIdCountRow(group.Key, group.Count()))
            .ToListAsync(cancellationToken);
    }

    public Task<IReadOnlyList<CarIdCountRow>> GetFavoriteCountsByCarAsync(
        CancellationToken cancellationToken = default) =>
        _db.Favorites.AsNoTracking()
            .GroupBy(favorite => favorite.CarId)
            .Select(group => new CarIdCountRow(group.Key, group.Count()))
            .ToListAsync(cancellationToken)
            .ContinueWith(task => (IReadOnlyList<CarIdCountRow>)task.Result, cancellationToken);

    public Task<int> CountActiveCarsAsync(string activeStatus, CancellationToken cancellationToken = default)
    {
        var statusLower = activeStatus.ToLower();
        return _db.Cars.AsNoTracking()
            .Where(car => car.Status.ToLower() == statusLower)
            .CountAsync(cancellationToken);
    }

    public Task<IReadOnlyList<Car>> GetLatestActiveCarsPageAsync(
        string activeStatus,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var statusLower = activeStatus.ToLower();
        var query = _db.Cars.AsNoTracking()
            .Where(car => car.Status.ToLower() == statusLower);
        return WithListIncludes(query
                .OrderByDescending(car => car.CreatedAt)
                .ThenByDescending(car => car.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize))
            .ToListAsync(cancellationToken)
            .ContinueWith(task => (IReadOnlyList<Car>)task.Result, cancellationToken);
    }

    public Task<int> CountCarViewsAsync(CancellationToken cancellationToken = default) =>
        _db.CarViews.CountAsync(cancellationToken);

    public Task<int> CountCarViewsSinceAsync(DateTime viewedAtFromUtc, CancellationToken cancellationToken = default) =>
        _db.CarViews.CountAsync(view => view.ViewedAt >= viewedAtFromUtc, cancellationToken);

    public Task<int> CountFavoritesAsync(CancellationToken cancellationToken = default) =>
        _db.Favorites.CountAsync(cancellationToken);

    public Task<IReadOnlyList<DateTime>> GetCarViewTimesSinceAsync(
        DateTime viewedAtFromUtc,
        CancellationToken cancellationToken = default) =>
        _db.CarViews.AsNoTracking()
            .Where(view => view.ViewedAt >= viewedAtFromUtc)
            .Select(view => view.ViewedAt)
            .ToListAsync(cancellationToken)
            .ContinueWith(task => (IReadOnlyList<DateTime>)task.Result, cancellationToken);

    public async Task<IReadOnlyList<CarIdCreatedAtRow>> GetActiveUsedCarSummariesAsync(
        string activeStatus,
        CancellationToken cancellationToken = default)
    {
        var statusLower = activeStatus.ToLower();
        return await _db.Cars.AsNoTracking()
            .Where(car => car.Status.ToLower() == statusLower && !car.IsNew)
            .Select(car => new CarIdCreatedAtRow(car.Id, car.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CarIdCountRow>> GetCarViewCountsForCarsAsync(
        IReadOnlyCollection<int> carIds,
        DateTime? viewedAtFromUtc,
        CancellationToken cancellationToken = default)
    {
        var views = _db.CarViews.AsNoTracking().Where(view => carIds.Contains(view.CarId));
        if (viewedAtFromUtc != null)
        {
            views = views.Where(view => view.ViewedAt >= viewedAtFromUtc.Value);
        }

        return await views
            .GroupBy(view => view.CarId)
            .Select(group => new CarIdCountRow(group.Key, group.Count()))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<int, DateTime>> GetActiveCarCreatedAtByIdsAsync(
        string activeStatus,
        IReadOnlyCollection<int> carIds,
        CancellationToken cancellationToken = default)
    {
        var statusLower = activeStatus.ToLower();
        return await _db.Cars.AsNoTracking()
            .Where(car =>
                carIds.Contains(car.Id)
                && car.Status.ToLower() == statusLower)
            .Select(car => new { car.Id, car.CreatedAt })
            .ToDictionaryAsync(car => car.Id, car => car.CreatedAt, cancellationToken);
    }

    public async Task<IReadOnlyList<Car>> GetCarsWithDetailsByIdsAsync(
        IReadOnlyCollection<int> carIds,
        CancellationToken cancellationToken = default) =>
        await WithListIncludes(CarsAsNoTracking().Where(car => carIds.Contains(car.Id)))
            .ToListAsync(cancellationToken);

    private static IQueryable<Car> ApplyFilters(IQueryable<Car> cars, CarListQueryCriteria query)
    {
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var terms = query.Search.Trim().ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var term in terms)
            {
                var token = term;
                if (query.IncludeListingText)
                {
                    cars = cars.Where(car =>
                        car.Brand.Name.ToLower().Contains(token)
                        || car.Model.ToLower().Contains(token)
                        || car.Description.ToLower().Contains(token)
                        || car.Color.ToLower().Contains(token)
                        || (car.Features != null && car.Features.ToLower().Contains(token))
                        || car.Category.Name.ToLower().Contains(token));
                }
                else
                {
                    cars = cars.Where(car =>
                        car.Brand.Name.ToLower().Contains(token)
                        || car.Model.ToLower().Contains(token)
                        || car.Description.ToLower().Contains(token));
                }
            }
        }

        if (query.BrandId is int brandId)
        {
            cars = cars.Where(car => car.BrandId == brandId);
        }

        if (query.CategoryId is int categoryId)
        {
            cars = cars.Where(car => car.CategoryId == categoryId);
        }

        if (!string.IsNullOrWhiteSpace(query.Model))
        {
            var model = query.Model.Trim().ToLower();
            cars = cars.Where(car => car.Model.ToLower() == model);
        }

        if (query.MinPrice is decimal minPrice)
        {
            cars = cars.Where(car => car.Price >= minPrice);
        }

        if (query.MaxPrice is decimal maxPrice)
        {
            cars = cars.Where(car => car.Price <= maxPrice);
        }

        if (query.MinYear is int minYear)
        {
            cars = cars.Where(car => car.Year >= minYear);
        }

        if (query.MaxYear is int maxYear)
        {
            cars = cars.Where(car => car.Year <= maxYear);
        }

        if (query.MinMileage is int minMileage)
        {
            cars = cars.Where(car => car.Mileage >= minMileage);
        }

        if (query.MaxMileage is int maxMileage)
        {
            cars = cars.Where(car => car.Mileage <= maxMileage);
        }

        var selectedSeats = query.SelectedSeats;
        if (selectedSeats.Count == 1)
        {
            var seats = selectedSeats[0];
            cars = cars.Where(car => car.Seats == seats);
        }
        else if (selectedSeats.Count > 1)
        {
            cars = cars.Where(car => car.Seats.HasValue && selectedSeats.Contains(car.Seats.Value));
        }

        if (!string.IsNullOrWhiteSpace(query.Fuel))
        {
            var fuel = query.Fuel.Trim().ToLower();
            cars = cars.Where(car => car.FuelType.ToLower() == fuel);
        }

        if (!string.IsNullOrWhiteSpace(query.Transmission))
        {
            var transmission = query.Transmission.Trim().ToLower();
            cars = cars.Where(car => car.Transmission.ToLower() == transmission);
        }

        if (query.IsNew is bool isNew)
        {
            cars = cars.Where(car => car.IsNew == isNew);
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var status = query.Status.Trim().ToLower();
            cars = cars.Where(car => car.Status.ToLower() == status);
        }

        return cars;
    }

    private static IQueryable<Car> ApplySort(IQueryable<Car> cars, string? sort)
    {
        var mode = sort?.Trim().ToLowerInvariant();
        return mode switch
        {
            "latest" => cars
                .OrderByDescending(car => car.CreatedAt)
                .ThenByDescending(car => car.Id),
            "price-asc" => cars
                .OrderBy(car => car.Price)
                .ThenBy(car => car.Id),
            "price-desc" => cars
                .OrderByDescending(car => car.Price)
                .ThenByDescending(car => car.Id),
            "year-desc" => cars
                .OrderByDescending(car => car.Year)
                .ThenByDescending(car => car.Id),
            "year-asc" => cars
                .OrderBy(car => car.Year)
                .ThenBy(car => car.Id),
            _ => cars
                .OrderBy(car => car.Brand.Name)
                .ThenBy(car => car.Model)
                .ThenByDescending(car => car.Year)
                .ThenByDescending(car => car.CreatedAt)
                .ThenBy(car => car.Id)
        };
    }
}
