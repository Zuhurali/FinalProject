using System.ComponentModel.DataAnnotations;

namespace FinalProject.Models.Entities;

public class ProductRating : BaseEntity
{
    public int ProductId { get; set; }

    [Required]
    [MaxLength(450)]
    public string UserId { get; set; } = string.Empty;

    [Range(1, 5)]
    public int Rating { get; set; }

    public bool IsDemo { get; set; }
}
