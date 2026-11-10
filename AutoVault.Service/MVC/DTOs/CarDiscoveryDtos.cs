namespace FinalProject.MVC.DTOs;

public class RankedCarDto
{
    public int Count { get; set; }

    public CarApiDto Car { get; set; } = new();
}

public class PagedRankedCarsDto
{
    public List<RankedCarDto> Items { get; set; } = new();

    public int CurrentPage { get; set; }

    public int PageSize { get; set; }

    public int TotalCount { get; set; }

    public int TotalPages { get; set; }
}

public class DailyViewPointDto
{
    public string Date { get; set; } = string.Empty;

    public int Views { get; set; }
}

public class CarDiscoveryStatisticsDto
{
    public int TotalViews { get; set; }

    public int ViewsToday { get; set; }

    public int ViewsLast7Days { get; set; }

    public int TotalFavorites { get; set; }

    public List<DailyViewPointDto> DailyViews { get; set; } = new();

    public List<RankedCarDto> MostViewed { get; set; } = new();

    public List<RankedCarDto> MostFavorited { get; set; } = new();
}
