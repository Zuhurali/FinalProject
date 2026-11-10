using System.ComponentModel.DataAnnotations;

namespace FinalProject.Models.Entities;

public sealed class Favorite : BaseEntity
{
    [Required]
    [MaxLength(450)]
    public string UserId { get; set; } = string.Empty;

    public int CarId { get; set; }

    public IdentityUserReference User { get; set; } = null!;

    public Car Car { get; set; } = null!;
}
