namespace FinalProject.Models.Entities;

/// <summary>MVC-owned table; mapped for sale pricing without owning migrations.</summary>
public class ProductCampaignReference
{
    public int Id { get; set; }

    public int? ProductId { get; set; }

    public bool AppliesToAllParts { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal? DiscountedPrice { get; set; }

    public decimal? DiscountPercent { get; set; }

    public DateTime StartsAt { get; set; }

    public DateTime EndsAt { get; set; }

    public bool IsActive { get; set; } = true;
}
