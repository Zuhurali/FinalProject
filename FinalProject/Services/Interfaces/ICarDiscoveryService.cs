using FinalProject.DTOs;

namespace FinalProject.Services.Interfaces;

public interface ICarDiscoveryService
{
    Task<bool> RecordViewAsync(int carId, DateTime? viewedAtUtc = null, CancellationToken cancellationToken = default);

    Task<PagedResult<RankedCarResponse>> GetMostViewedAsync(
        string period,
        int page = 1,
        int pageSize = 9,
        DateTime? utcNow = null,
        CancellationToken cancellationToken = default);

    Task<PagedResult<RankedCarResponse>> GetMostFavoritedAsync(
        int page = 1,
        int pageSize = 9,
        CancellationToken cancellationToken = default);

    Task<PagedResult<CarResponse>> GetLatestArrivalsAsync(
        int page = 1,
        int pageSize = 9,
        CancellationToken cancellationToken = default);

    Task<CarDiscoveryStatisticsResponse> GetStatisticsAsync(
        DateTime? utcNow = null,
        CancellationToken cancellationToken = default);
}
