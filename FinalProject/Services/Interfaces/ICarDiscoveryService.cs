using FinalProject.DTOs;

namespace FinalProject.Services.Interfaces;

public interface ICarDiscoveryService
{
    Task<bool> RecordViewAsync(int carId, DateTime? viewedAtUtc = null, CancellationToken cancellationToken = default);

    Task<PagedResultDto<RankedCarResponseDto>> GetMostViewedAsync(
        string period,
        int page = 1,
        int pageSize = 9,
        DateTime? utcNow = null,
        bool usedOnly = false,
        CancellationToken cancellationToken = default);

    Task<PagedResultDto<RankedCarResponseDto>> GetMostFavoritedAsync(
        int page = 1,
        int pageSize = 9,
        CancellationToken cancellationToken = default);

    Task<PagedResultDto<CarResponseDto>> GetLatestArrivalsAsync(
        int page = 1,
        int pageSize = 9,
        CancellationToken cancellationToken = default);

    Task<CarDiscoveryStatisticsResponseDto> GetStatisticsAsync(
        DateTime? utcNow = null,
        CancellationToken cancellationToken = default);
}
