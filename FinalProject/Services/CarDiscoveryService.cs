using FinalProject.Commerce;
using FinalProject.Data;
using FinalProject.DTOs;
using FinalProject.Mappings;
using FinalProject.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinalProject.Services;

public sealed class CarDiscoveryService : ICarDiscoveryService
{
    private readonly FinalProjectDbContext _db;

    public CarDiscoveryService(FinalProjectDbContext db)
    {
        _db = db;
    }

    public async Task<bool> RecordViewAsync(int carId, DateTime? viewedAtUtc = null, CancellationToken cancellationToken = default)
    {
        if (carId <= 0 || !await _db.Cars.AnyAsync(car => car.Id == carId, cancellationToken))
        {
            return false;
        }

        var viewedAt = viewedAtUtc ?? DateTime.UtcNow;
        _db.CarViews.Add(new Models.Entities.CarView
        {
            CarId = carId,
            ViewedAt = DateTime.SpecifyKind(viewedAt, DateTimeKind.Utc)
        });
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<PagedResult<RankedCarResponse>> GetMostViewedAsync(
        string period,
        int page = 1,
        int pageSize = 9,
        DateTime? utcNow = null,
        CancellationToken cancellationToken = default)
    {
        if (!CarDiscoveryRules.TryNormalizePeriod(period, out var normalized))
        {
            throw new ArgumentException("Unknown view period.", nameof(period));
        }

        (page, pageSize) = CarDiscoveryRules.NormalizePaging(page, pageSize);
        var views = _db.CarViews.AsNoTracking();
        var start = CarDiscoveryRules.PeriodStartUtc(normalized, utcNow ?? DateTime.UtcNow);
        if (start != null)
        {
            views = views.Where(view => view.ViewedAt >= start.Value);
        }

        var counts = await views
            .GroupBy(view => view.CarId)
            .Select(group => new { CarId = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);

        return await RankPageAsync(counts.Select(item => (item.CarId, item.Count)), page, pageSize, cancellationToken);
    }

    public async Task<PagedResult<RankedCarResponse>> GetMostFavoritedAsync(
        int page = 1,
        int pageSize = 9,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = CarDiscoveryRules.NormalizePaging(page, pageSize);
        var counts = await _db.Favorites
            .AsNoTracking()
            .GroupBy(favorite => favorite.CarId)
            .Select(group => new { CarId = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);

        return await RankPageAsync(counts.Select(item => (item.CarId, item.Count)), page, pageSize, cancellationToken);
    }

    public async Task<PagedResult<CarResponse>> GetLatestArrivalsAsync(
        int page = 1,
        int pageSize = 9,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = CarDiscoveryRules.NormalizePaging(page, pageSize);
        var query = _db.Cars.AsNoTracking();
        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);
        if (totalPages > 0 && page > totalPages)
        {
            page = totalPages;
        }

        IReadOnlyList<CarResponse> items = Array.Empty<CarResponse>();
        if (totalCount > 0)
        {
            var cars = await query
                .OrderByDescending(car => car.CreatedAt)
                .ThenByDescending(car => car.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
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
            CurrentPage = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };
    }

    public async Task<CarDiscoveryStatisticsResponse> GetStatisticsAsync(
        DateTime? utcNow = null,
        CancellationToken cancellationToken = default)
    {
        var now = utcNow ?? DateTime.UtcNow;
        var todayStart = CarDiscoveryRules.PeriodStartUtc(CarDiscoveryRules.Today, now);
        var weekStart = CarDiscoveryRules.PeriodStartUtc(CarDiscoveryRules.Week, now);
        var trendStartLocal = CarDiscoveryRules.LocalDate(now).AddDays(-13);
        var trendStartUtc = TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(trendStartLocal, DateTimeKind.Unspecified),
            CarDiscoveryRules.DealershipZone);

        var viewTimes = await _db.CarViews
            .AsNoTracking()
            .Where(view => view.ViewedAt >= trendStartUtc)
            .Select(view => view.ViewedAt)
            .ToListAsync(cancellationToken);
        var byDay = viewTimes
            .GroupBy(CarDiscoveryRules.LocalDate)
            .ToDictionary(group => group.Key, group => group.Count());
        var daily = new List<DailyViewPoint>();
        for (var day = trendStartLocal; day <= CarDiscoveryRules.LocalDate(now); day = day.AddDays(1))
        {
            daily.Add(new DailyViewPoint
            {
                Date = day.ToString("yyyy-MM-dd"),
                Views = byDay.GetValueOrDefault(day)
            });
        }

        var mostViewed = await GetMostViewedAsync(CarDiscoveryRules.All, page: 1, pageSize: 5, utcNow: now, cancellationToken: cancellationToken);
        var mostFavorited = await GetMostFavoritedAsync(page: 1, pageSize: 5, cancellationToken: cancellationToken);

        return new CarDiscoveryStatisticsResponse
        {
            TotalViews = await _db.CarViews.CountAsync(cancellationToken),
            ViewsToday = await _db.CarViews.CountAsync(view => view.ViewedAt >= todayStart, cancellationToken),
            ViewsLast7Days = await _db.CarViews.CountAsync(view => view.ViewedAt >= weekStart, cancellationToken),
            TotalFavorites = await _db.Favorites.CountAsync(cancellationToken),
            DailyViews = daily,
            MostViewed = mostViewed.Items.ToList(),
            MostFavorited = mostFavorited.Items.ToList()
        };
    }

    private async Task<PagedResult<RankedCarResponse>> RankPageAsync(
        IEnumerable<(int CarId, int Count)> counts,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var rankedIds = counts.Where(item => item.Count > 0).ToList();
        if (rankedIds.Count == 0)
        {
            return EmptyRankedPage(page, pageSize);
        }

        var carIds = rankedIds.Select(item => item.CarId).ToList();
        var createdAt = await _db.Cars
            .AsNoTracking()
            .Where(car => carIds.Contains(car.Id))
            .Select(car => new { car.Id, car.CreatedAt })
            .ToDictionaryAsync(car => car.Id, car => car.CreatedAt, cancellationToken);

        var ordered = rankedIds
            .Where(item => createdAt.ContainsKey(item.CarId))
            .OrderByDescending(item => item.Count)
            .ThenByDescending(item => createdAt[item.CarId])
            .ThenByDescending(item => item.CarId)
            .ToList();

        var totalCount = ordered.Count;
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        if (page > totalPages)
        {
            page = totalPages;
        }

        var pageSlice = ordered.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        var pageIds = pageSlice.Select(item => item.CarId).ToList();
        var cars = await _db.Cars
            .AsNoTracking()
            .Where(car => pageIds.Contains(car.Id))
            .Include(car => car.Brand)
            .Include(car => car.Category)
            .Include(car => car.Owner)
            .Include(car => car.Images)
            .AsSplitQuery()
            .ToDictionaryAsync(car => car.Id, cancellationToken);

        var pageItems = pageSlice
            .Where(item => cars.ContainsKey(item.CarId))
            .Select(item => new RankedCarResponse
            {
                Count = item.Count,
                Car = cars[item.CarId].ToResponse()
            })
            .ToList();

        return new PagedResult<RankedCarResponse>
        {
            Items = pageItems,
            CurrentPage = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };
    }

    private static PagedResult<RankedCarResponse> EmptyRankedPage(int page, int pageSize) =>
        new()
        {
            Items = Array.Empty<RankedCarResponse>(),
            CurrentPage = page,
            PageSize = pageSize,
            TotalCount = 0,
            TotalPages = 0
        };
}
