using FinalProject.Data;
using FinalProject.DTOs;
using FinalProject.Mappings;
using FinalProject.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinalProject.Services;

public class CarService : ICarService
{
    private readonly FinalProjectDbContext _dbContext;

    public CarService(FinalProjectDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<CarResponse>> QueryAsync(CarListQuery query, CancellationToken cancellationToken = default)
    {
        var filtered = ApplyFilters(_dbContext.Cars.AsNoTracking(), query);
        var totalCount = await filtered.CountAsync(cancellationToken);
        var totalPages = totalCount == 0
            ? 0
            : (int)Math.Ceiling(totalCount / (double)query.PageSize);

        IReadOnlyList<CarResponse> items = Array.Empty<CarResponse>();
        if (totalCount > 0 && query.Page <= totalPages)
        {
            var cars = await filtered
                .OrderBy(car => car.Brand.Name)
                .ThenBy(car => car.Model)
                .ThenByDescending(car => car.Year)
                .ThenByDescending(car => car.CreatedAt)
                .ThenBy(car => car.Id)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Include(car => car.Brand)
                .Include(car => car.Category)
                .Include(car => car.Owner)
                .Include(car => car.Images)
                .AsSplitQuery()
                .ToListAsync(cancellationToken);

            items = cars.Select(car => car.ToResponse()).ToList();
        }

        return new PagedResult<CarResponse>
        {
            Items = items,
            CurrentPage = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };
    }

    public async Task<IReadOnlyList<CarSuggestionResponse>> GetSuggestionsAsync(string query, int limit, CancellationToken cancellationToken = default)
    {
        var term = query.Trim().ToLower();
        var brandNames = await _dbContext.Brands
            .AsNoTracking()
            .Where(brand => brand.Name.ToLower().Contains(term))
            .Select(brand => brand.Name)
            .Distinct()
            .ToListAsync(cancellationToken);

        var carNames = await _dbContext.Cars
            .AsNoTracking()
            .Where(car =>
                car.Description.ToLower().Contains(term)
                || car.Model.ToLower().Contains(term)
                || car.Brand.Name.ToLower().Contains(term))
            .Select(car => car.Description)
            .Where(name => name != "")
            .Distinct()
            .ToListAsync(cancellationToken);

        return brandNames
            .Select(name => new CarSuggestionResponse { Text = name, Type = "brand" })
            .Concat(carNames
                .Where(name => !brandNames.Contains(name, StringComparer.OrdinalIgnoreCase))
                .Select(name => new CarSuggestionResponse { Text = name, Type = "car" }))
            .OrderBy(item => item.Type == "brand" ? 0 : 1)
            .ThenBy(item => SuggestionRank(item.Text, term))
            .ThenBy(item => item.Text, StringComparer.OrdinalIgnoreCase)
            .Take(limit)
            .ToList();
    }

    public async Task<CarResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var car = await _dbContext.Cars
            .AsNoTracking()
            .Include(item => item.Brand)
            .Include(item => item.Category)
            .Include(item => item.Owner)
            .Include(item => item.Images)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        return car?.ToResponse();
    }

    public async Task<IReadOnlyList<CarResponse>> GetOwnedAsync(
        string ownerId,
        CancellationToken cancellationToken = default)
    {
        var cars = await _dbContext.Cars
            .AsNoTracking()
            .Where(car => car.OwnerId == ownerId)
            .OrderByDescending(car => car.CreatedAt)
            .Include(car => car.Brand)
            .Include(car => car.Category)
            .Include(car => car.Owner)
            .Include(car => car.Images)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        return cars.Select(car => car.ToResponse()).ToList();
    }

    public async Task<CarResponse?> CreateAsync(
        string ownerId,
        SaveCarRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await _dbContext.IdentityUsers.AnyAsync(user => user.Id == ownerId, cancellationToken)
            || !await LookupsExistAsync(request.BrandId, request.CategoryId, cancellationToken))
        {
            return null;
        }

        var now = DateTime.UtcNow;
        var car = new Models.Entities.Car
        {
            BrandId = request.BrandId,
            CategoryId = request.CategoryId,
            Model = request.Model.Trim(),
            Year = request.Year,
            Price = request.Price,
            Mileage = request.Mileage,
            FuelType = request.FuelType.Trim(),
            Transmission = request.Transmission.Trim(),
            Color = request.Color.Trim(),
            Description = request.Description.Trim(),
            OwnerId = ownerId,
            Status = request.Status,
            IsNew = true,
            CreatedAt = now,
            Images =
            {
                new Models.Entities.CarImage
                {
                    ImageUrl = request.ImageUrl.Trim(),
                    IsMain = true,
                    DisplayOrder = 0
                }
            }
        };

        _dbContext.Cars.Add(car);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(car.Id, cancellationToken);
    }

    public async Task<CarResponse?> CreateCatalogAsync(
        SaveCarRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await LookupsExistAsync(request.BrandId, request.CategoryId, cancellationToken))
        {
            return null;
        }

        var now = DateTime.UtcNow;
        var car = new Models.Entities.Car
        {
            BrandId = request.BrandId,
            CategoryId = request.CategoryId,
            Model = request.Model.Trim(),
            Year = request.Year,
            Price = request.Price,
            Mileage = request.Mileage,
            FuelType = request.FuelType.Trim(),
            Transmission = request.Transmission.Trim(),
            Color = request.Color.Trim(),
            Description = request.Description.Trim(),
            OwnerId = null,
            Status = request.Status,
            IsNew = true,
            CreatedAt = now,
            Images =
            {
                new Models.Entities.CarImage
                {
                    ImageUrl = request.ImageUrl.Trim(),
                    IsMain = true,
                    DisplayOrder = 0
                }
            }
        };

        _dbContext.Cars.Add(car);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(car.Id, cancellationToken);
    }

    public async Task<CarResponse?> UpdateAsync(
        int id,
        SaveCarRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await LookupsExistAsync(request.BrandId, request.CategoryId, cancellationToken))
        {
            return null;
        }

        var car = await _dbContext.Cars
            .Include(item => item.Images)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (car == null)
        {
            return null;
        }

        car.BrandId = request.BrandId;
        car.CategoryId = request.CategoryId;
        car.Model = request.Model.Trim();
        car.Year = request.Year;
        car.Price = request.Price;
        car.Mileage = request.Mileage;
        car.FuelType = request.FuelType.Trim();
        car.Transmission = request.Transmission.Trim();
        car.Color = request.Color.Trim();
        car.Description = request.Description.Trim();
        car.Status = request.Status;
        car.UpdatedAt = DateTime.UtcNow;

        foreach (var existingImage in car.Images)
        {
            existingImage.IsMain = false;
        }

        var image = car.Images.OrderBy(item => item.DisplayOrder).ThenBy(item => item.Id).FirstOrDefault();
        if (image == null)
        {
            car.Images.Add(new Models.Entities.CarImage
            {
                ImageUrl = request.ImageUrl.Trim(),
                IsMain = true,
                DisplayOrder = 0
            });
        }
        else
        {
            image.ImageUrl = request.ImageUrl.Trim();
            image.IsMain = true;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var car = await _dbContext.Cars.FindAsync([id], cancellationToken);
        if (car == null)
        {
            return false;
        }

        _dbContext.Cars.Remove(car);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<CarLookupResponse> GetLookupsAsync(CancellationToken cancellationToken = default)
    {
        var brands = await _dbContext.Brands
            .AsNoTracking()
            .OrderBy(brand => brand.Name)
            .Select(brand => new BrandResponse
            {
                Id = brand.Id,
                Name = brand.Name,
                CreatedAt = brand.CreatedAt
            })
            .ToListAsync(cancellationToken);
        var categories = await _dbContext.Categories
            .AsNoTracking()
            .OrderBy(category => category.Name)
            .Select(category => new CategoryResponse
            {
                Id = category.Id,
                Name = category.Name
            })
            .ToListAsync(cancellationToken);

        return new CarLookupResponse(brands, categories);
    }

    private async Task<bool> LookupsExistAsync(
        int brandId,
        int categoryId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Brands.AnyAsync(brand => brand.Id == brandId, cancellationToken)
            && await _dbContext.Categories.AnyAsync(category => category.Id == categoryId, cancellationToken);
    }

    private static IQueryable<Models.Entities.Car> ApplyFilters(IQueryable<Models.Entities.Car> cars, CarListQuery query)
    {
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var terms = query.Search.Trim().ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var term in terms)
            {
                var token = term;
                cars = cars.Where(car =>
                    car.Brand.Name.ToLower().Contains(token)
                    || car.Model.ToLower().Contains(token)
                    || car.Description.ToLower().Contains(token));
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

    private static int SuggestionRank(string text, string term)
    {
        var value = text.ToLowerInvariant();
        if (value.StartsWith(term, StringComparison.Ordinal))
        {
            return 0;
        }

        return value.Contains(term, StringComparison.Ordinal) ? 1 : 2;
    }
}
