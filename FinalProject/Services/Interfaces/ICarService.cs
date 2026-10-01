using FinalProject.DTOs;

namespace FinalProject.Services.Interfaces;

public interface ICarService
{
    Task<IReadOnlyList<CarResponse>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<CarResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
}
