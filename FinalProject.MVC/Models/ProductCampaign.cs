using System.ComponentModel.DataAnnotations;

namespace FinalProject.MVC.Models;

public class ProductCampaign
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public ShopCatalogProduct? Product { get; set; }

    [Required, MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    public decimal? DiscountedPrice { get; set; }

    public decimal? DiscountPercent { get; set; }

    public DateTime StartsAt { get; set; }

    public DateTime EndsAt { get; set; }

    public bool IsActive { get; set; } = true;
}
