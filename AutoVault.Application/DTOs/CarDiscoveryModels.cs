namespace FinalProject.DTOs;

public class RankedCarResponseDto
{
    public int Count { get; set; }

    public CarResponseDto Car { get; set; } = null!;
}

public class DailyViewPointDto
{
    public string Date { get; set; } = string.Empty;

    public int Views { get; set; }
}

public class CarDiscoveryStatisticsResponseDto
{
    public int TotalViews { get; set; }

    public int ViewsToday { get; set; }

    public int ViewsLast7Days { get; set; }

    public int TotalFavorites { get; set; }

    public List<DailyViewPointDto> DailyViews { get; set; } = new();

    public List<RankedCarResponseDto> MostViewed { get; set; } = new();

    public List<RankedCarResponseDto> MostFavorited { get; set; } = new();
}
