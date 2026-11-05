namespace FinalProject.Models.Entities;

/// <summary>MVC-owned table; mapped for admin API read/write without owning migrations.</summary>
public class InPersonSaleReference : BaseEntity
{
    public int ProductId { get; set; }

    public ShopProductReference? Product { get; set; }

    /// <summary>When set, this sale completed the linked showroom purchase request.</summary>
    public int? PurchaseRequestId { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal ListUnitPrice { get; set; }

    public int? CampaignId { get; set; }

    public decimal? CampaignDiscountPercent { get; set; }

    public string RecordedByUserId { get; set; } = string.Empty;

    public string Note { get; set; } = string.Empty;

    /// <summary>Durable client/submission key so retries do not create duplicate sales.</summary>
    public string? IdempotencyKey { get; set; }

    public DateTime SoldAt { get; set; }

    public bool IsDemo { get; set; }

    public DateTime? VoidedAt { get; set; }

    public string? VoidedByUserId { get; set; }
}
