using System.ComponentModel.DataAnnotations;
using FinalProject.Commerce;

namespace FinalProject.Models.Entities;

public class ProductReview
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    [Required]
    [MaxLength(450)]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [MaxLength(ReviewContentRules.MaxLength)]
    public string Content { get; set; } = string.Empty;

    public bool IsHidden { get; set; }

    public bool IsEdited { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public bool IsDemo { get; set; }
}
