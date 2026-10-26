using System.ComponentModel.DataAnnotations;

namespace FinalProject.Models.Entities;

/// <summary>Signed-in user's favorite spare part (ShopProducts).</summary>
public sealed class ProductFavorite
{
    public int Id { get; set; }

    [Required]
    [MaxLength(450)]
    public string UserId { get; set; } = string.Empty;

    public int ProductId { get; set; }

    public DateTime CreatedAt { get; set; }

    public IdentityUserReference User { get; set; } = null!;

    public ShopProductReference? Product { get; set; }
}
