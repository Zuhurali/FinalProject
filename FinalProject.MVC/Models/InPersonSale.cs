using System.ComponentModel.DataAnnotations;

namespace FinalProject.MVC.Models;

public class InPersonSale
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public ShopCatalogProduct? Product { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    [Required, MaxLength(450)]
    public string RecordedByUserId { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Note { get; set; } = string.Empty;

    public DateTime SoldAt { get; set; }
}
