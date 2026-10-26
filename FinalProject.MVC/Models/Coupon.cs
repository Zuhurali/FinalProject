using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinalProject.MVC.Models;

public class Coupon
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Percent off eligible cart subtotal (1–100). Server calculates the amount.</summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal DiscountPercent { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>UTC expiry; null means no expiry.</summary>
    public DateTime? ExpiresAt { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal MinimumPurchaseAmount { get; set; }

    /// <summary>How many times one user may redeem (persisted usages). Default 1.</summary>
    public int MaxUsesPerUser { get; set; } = 1;

    /// <summary>Optional global redemption cap across all users.</summary>
    public int? MaxTotalUses { get; set; }

    /// <summary>When false, only <see cref="ProductId"/> is eligible.</summary>
    public bool AppliesToAllParts { get; set; } = true;

    public int? ProductId { get; set; }

    public ShopCatalogProduct? Product { get; set; }

    public DateTime CreatedAt { get; set; }
}
