namespace FinalProject.DTOs;

public class RankedCarResponse
{
    public int Count { get; set; }

    public CarResponse Car { get; set; } = null!;
}

public class DailyViewPoint
{
    public string Date { get; set; } = string.Empty;

    public int Views { get; set; }
}

public class CarDiscoveryStatisticsResponse
{
    public int TotalViews { get; set; }

    public int ViewsToday { get; set; }

    public int ViewsLast7Days { get; set; }

    public int TotalFavorites { get; set; }

    public List<DailyViewPoint> DailyViews { get; set; } = new();

    public List<RankedCarResponse> MostViewed { get; set; } = new();

    public List<RankedCarResponse> MostFavorited { get; set; } = new();
}
