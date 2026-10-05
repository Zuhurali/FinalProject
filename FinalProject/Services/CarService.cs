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
                .OrderByDescending(car => car.CreatedAt)
                .ThenBy(car => car.Id)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Include(car => car.Brand)
                .Include(car => car.Category)
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
            .Include(item => item.Images)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        return car?.ToResponse();
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
