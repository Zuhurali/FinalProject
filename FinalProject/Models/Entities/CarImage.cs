using System.ComponentModel.DataAnnotations;

namespace FinalProject.Models.Entities;

public class CarImage
{
    public int Id { get; set; }

    public int CarId { get; set; }

    [Required]
    [MaxLength(500)]
    public string ImageUrl { get; set; } = string.Empty;

    public bool IsMain { get; set; }

    public int DisplayOrder { get; set; }

    public Car Car { get; set; } = null!;
}
