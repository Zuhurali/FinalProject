using System.ComponentModel.DataAnnotations;

namespace FinalProject.Models.Entities;

public sealed class CarMessage
{
    public int Id { get; set; }

    public int ConversationId { get; set; }

    [Required]
    [MaxLength(450)]
    public string SenderId { get; set; } = string.Empty;

    [Required]
    [MaxLength(450)]
    public string ReceiverId { get; set; } = string.Empty;

    [Required]
    [MaxLength(4000)]
    public string Content { get; set; } = string.Empty;

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; }

    public Conversation Conversation { get; set; } = null!;

    public IdentityUserReference Sender { get; set; } = null!;

    public IdentityUserReference Receiver { get; set; } = null!;
}
