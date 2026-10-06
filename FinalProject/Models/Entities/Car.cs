using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinalProject.Models.Entities;

public class Car
{
    public int Id { get; set; }

    [Range(1, int.MaxValue)]
    public int BrandId { get; set; }

    [Range(1, int.MaxValue)]
    public int CategoryId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Model { get; set; } = string.Empty;

    [Range(1900, 2100)]
    public int Year { get; set; }

    [Range(0.01, 999999999)]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }

    [Range(0, int.MaxValue)]
    public int Mileage { get; set; }

    [Required]
    [MaxLength(50)]
    public string FuelType { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Transmission { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Color { get; set; } = string.Empty;

    [MaxLength(4000)]
    public string Description { get; set; } = string.Empty;

    public bool IsNew { get; set; }

    public bool IsFeatured { get; set; }

    [MaxLength(450)]
    public string? OwnerId { get; set; }

    [Required]
    [MaxLength(30)]
    public string Status { get; set; } = "Active";

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Brand Brand { get; set; } = null!;

    public Category Category { get; set; } = null!;

    public IdentityUserReference? Owner { get; set; }

    public ICollection<CarImage> Images { get; set; } = new List<CarImage>();

    public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();

    public ICollection<Conversation> Conversations { get; set; } = new List<Conversation>();
}
