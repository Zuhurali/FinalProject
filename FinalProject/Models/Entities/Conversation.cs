using System.ComponentModel.DataAnnotations;

namespace FinalProject.Models.Entities;

public sealed class Conversation
{
    public int Id { get; set; }

    public int CarId { get; set; }

    [Required]
    [MaxLength(450)]
    public string BuyerId { get; set; } = string.Empty;

    [Required]
    [MaxLength(450)]
    public string SellerId { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Subject { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public Car Car { get; set; } = null!;

    public IdentityUserReference Buyer { get; set; } = null!;

    public IdentityUserReference Seller { get; set; } = null!;

    public ICollection<CarMessage> Messages { get; set; } = new List<CarMessage>();
}
