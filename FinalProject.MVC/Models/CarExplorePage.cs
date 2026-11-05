namespace FinalProject.MVC.Models;

public class CarExplorePage
{
    public const int PageSize = 9;

    public const int UsedPageSize = PageSize;

    public string Heading { get; set; } = string.Empty;

    public string Intro { get; set; } = string.Empty;

    public string Kind { get; set; } = string.Empty;

    public string Period { get; set; } = "all";

    public string CountLabel { get; set; } = string.Empty;

    public string ActionName { get; set; } = string.Empty;

    public string? Condition { get; set; }

    public bool ApiUnavailable { get; set; }

    public int Page { get; set; } = 1;

    public int TotalPages { get; set; }

    public int TotalCount { get; set; }

    public List<CarExploreItem> Cars { get; set; } = new();
}

public class CarExploreItem
{
    public Car Car { get; set; } = new();

    public int? Count { get; set; }

    public string? Detail { get; set; }
}
