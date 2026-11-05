using System.ComponentModel.DataAnnotations;

namespace FinalProject.Models.Entities;

public class ProductReviewRevision : BaseEntity
{
    public int ReviewId { get; set; }

    [Required]
    [MaxLength(1000)]
    public string PreviousContent { get; set; } = string.Empty;

    public DateTime EditedAt { get; set; }
}
