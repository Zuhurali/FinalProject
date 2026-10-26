using System.ComponentModel.DataAnnotations;

namespace FinalProject.MVC.Models;

public class InPersonSale
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public ShopCatalogProduct? Product { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    /// <summary>Unit price charged in the store (campaign price when a live offer applied).</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>Catalog list price at the time of sale (unchanged base price).</summary>
    public decimal ListUnitPrice { get; set; }

    public int? CampaignId { get; set; }

    public ProductCampaign? Campaign { get; set; }

    public decimal? CampaignDiscountPercent { get; set; }

    [Required, MaxLength(450)]
    public string RecordedByUserId { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Note { get; set; } = string.Empty;

    public DateTime SoldAt { get; set; }

    public bool IsDemo { get; set; }

    /// <summary>When set, the sale was voided and stock was restored.</summary>
    public DateTime? VoidedAt { get; set; }

    [MaxLength(450)]
    public string? VoidedByUserId { get; set; }
}
