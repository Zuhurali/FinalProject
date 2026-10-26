namespace FinalProject.Models.Entities;

/// <summary>MVC-owned table; mapped for admin API read/write without owning migrations.</summary>
public class InPersonSaleReference
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public ShopProductReference? Product { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal ListUnitPrice { get; set; }

    public int? CampaignId { get; set; }

    public decimal? CampaignDiscountPercent { get; set; }

    public string RecordedByUserId { get; set; } = string.Empty;

    public string Note { get; set; } = string.Empty;

    public DateTime SoldAt { get; set; }

    public bool IsDemo { get; set; }

    public DateTime? VoidedAt { get; set; }

    public string? VoidedByUserId { get; set; }
}
