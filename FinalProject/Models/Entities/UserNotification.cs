using System.ComponentModel.DataAnnotations;

namespace FinalProject.Models.Entities;

public sealed class UserNotification : BaseEntity
{
    [Required]
    [MaxLength(450)]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [MaxLength(40)]
    public string Type { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(1000)]
    public string Body { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Url { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? DedupKey { get; set; }

    public bool IsRead { get; set; }

    public int? RelatedConversationId { get; set; }

    public int? RelatedCarId { get; set; }

    public int? RelatedMessageId { get; set; }

    public IdentityUserReference User { get; set; } = null!;
}
