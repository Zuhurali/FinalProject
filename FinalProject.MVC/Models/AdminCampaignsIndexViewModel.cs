namespace FinalProject.MVC.Models;

public sealed class AdminCampaignsIndexViewModel
{
    public IReadOnlyList<CampaignListItem> Items { get; set; } = Array.Empty<CampaignListItem>();

    public string? Search { get; set; }

    public string? Status { get; set; }

    public int TotalCount { get; set; }
}

public sealed class CampaignListItem
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public bool AppliesToAllParts { get; set; }

    public string ScopeLabel { get; set; } = string.Empty;

    public int? ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public decimal? OriginalPrice { get; set; }

    public decimal? DiscountedPrice { get; set; }

    public decimal? DiscountPercent { get; set; }

    public DateTime StartsAt { get; set; }

    public DateTime EndsAtInclusive { get; set; }

    public bool IsActive { get; set; }

    public string Status { get; set; } = string.Empty;
}
