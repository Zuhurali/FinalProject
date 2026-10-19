using System.ComponentModel.DataAnnotations;

namespace FinalProject.MVC.Models;

public class ProductCampaign
{
    public int Id { get; set; }

    /// <summary>
    /// Set for single-product campaigns. Null when <see cref="AppliesToAllParts"/> is true.
    /// </summary>
    public int? ProductId { get; set; }

    public ShopCatalogProduct? Product { get; set; }

    public bool AppliesToAllParts { get; set; }

    [MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    public decimal? DiscountedPrice { get; set; }

    public decimal? DiscountPercent { get; set; }

    public DateTime StartsAt { get; set; }

    public DateTime EndsAt { get; set; }

    public bool IsActive { get; set; } = true;
}
