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

    public async Task<IReadOnlyList<CarResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var cars = await CarsQuery()
            .OrderByDescending(car => car.CreatedAt)
            .ThenBy(car => car.Id)
            .ToListAsync(cancellationToken);

        return cars.Select(car => car.ToResponse()).ToList();
    }

    public async Task<CarResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var car = await CarsQuery()
            .FirstOrDefaultAsync(car => car.Id == id, cancellationToken);

        return car?.ToResponse();
    }

    private IQueryable<Models.Entities.Car> CarsQuery()
    {
        return _dbContext.Cars
            .AsNoTracking()
            .Include(car => car.Brand)
            .Include(car => car.Category)
            .Include(car => car.Images);
    }
}
