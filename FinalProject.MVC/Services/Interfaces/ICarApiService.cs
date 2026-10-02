using FinalProject.MVC.Models.Api;

namespace FinalProject.MVC.Services.Interfaces;

public interface ICarApiService
{
    Task<IReadOnlyList<CarApiDto>> GetCarsAsync(CancellationToken cancellationToken = default);

    Task<CarApiDto?> GetCarByIdAsync(int id, CancellationToken cancellationToken = default);
}
