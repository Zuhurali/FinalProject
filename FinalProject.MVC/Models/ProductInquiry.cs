using System.ComponentModel.DataAnnotations;

namespace FinalProject.MVC.Models;

public class ProductInquiry
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public ShopCatalogProduct? Product { get; set; }

    [Required, MaxLength(80)]
    public string CustomerName { get; set; } = string.Empty;

    [Required, MaxLength(200), EmailAddress]
    public string Email { get; set; } = string.Empty;

    [MaxLength(40)]
    public string? Phone { get; set; }

    [Required, MaxLength(1000)]
    public string Message { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public bool IsHandled { get; set; }
}
