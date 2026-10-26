using System.ComponentModel.DataAnnotations;

namespace FinalProject.MVC.Models;

public class ProductInquiry
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public ShopCatalogProduct? Product { get; set; }

    [Required, MaxLength(80)]
    public string CustomerName { get; set; } = string.Empty;

    [Required, MaxLength(200), EmailAddress]
    public string Email { get; set; } = string.Empty;

    [MaxLength(40)]
    public string? Phone { get; set; }

    [Required, MaxLength(1000)]
    public string Message { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public bool IsHandled { get; set; }

    /// <summary>Optional Identity user when the question was submitted while signed in.</summary>
    [MaxLength(450)]
    public string? UserId { get; set; }

    // ── Admin reply ──────────────────────────────────────────────────────────
    /// <summary>The admin's written reply. Null means no reply has been sent yet.</summary>
    [MaxLength(2000)]
    public string? AdminReply { get; set; }

    /// <summary>UTC timestamp of the most recent reply save.</summary>
    public DateTime? AdminRepliedAt { get; set; }

    /// <summary>Identity user ID of the admin who last saved the reply.</summary>
    [MaxLength(450)]
    public string? AdminUserId { get; set; }

    // Computed helpers (not mapped)
    public bool HasReply => !string.IsNullOrWhiteSpace(AdminReply);

    public string StatusLabel =>
        HasReply ? "Answered" :
        IsHandled ? "Closed" :
        "New";
}
