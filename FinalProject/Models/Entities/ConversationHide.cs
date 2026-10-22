using System.ComponentModel.DataAnnotations;

namespace FinalProject.Models.Entities;

/// <summary>
/// Per-user hide of a conversation from the Messages list.
/// Does not delete the conversation or any messages.
/// </summary>
public sealed class ConversationHide
{
    public int Id { get; set; }

    public int ConversationId { get; set; }

    [Required]
    [MaxLength(450)]
    public string UserId { get; set; } = string.Empty;

    public DateTime HiddenAt { get; set; }

    public Conversation Conversation { get; set; } = null!;

    public IdentityUserReference User { get; set; } = null!;
}
