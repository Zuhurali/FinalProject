using System.ComponentModel.DataAnnotations;

namespace FinalProject.MVC.Models;

public class ProductInquiry : BaseEntity
{
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

    public const string StatusNew = "New";
    public const string StatusAnswered = "Answered";
    public const string StatusClosed = "Closed";
    public const string StatusReopened = "Reopened";

    public const string LabelAwaitingReply = "Awaiting reply";
    public const string LabelAnswered = "Answered";
    public const string LabelClosed = "Closed";
    public const string LabelAwaitingFollowUp = "Awaiting follow-up";

    // Computed helpers (not mapped)
    public bool HasReply => !string.IsNullOrWhiteSpace(AdminReply);

    /// <summary>
    /// Reopened is an open question whose last change happened after the stored reply.
    /// That uses UpdatedAt and AdminRepliedAt, so no extra column is required.
    /// </summary>
    public bool IsReopened =>
        !IsHandled
        && HasReply
        && UpdatedAt.HasValue
        && AdminRepliedAt.HasValue
        && UpdatedAt.Value > AdminRepliedAt.Value;

    public string StatusKey =>
        IsHandled ? StatusClosed :
        !HasReply ? StatusNew :
        IsReopened ? StatusReopened :
        StatusAnswered;

    public string StatusLabel => StatusKey switch
    {
        StatusAnswered => LabelAnswered,
        StatusClosed => LabelClosed,
        StatusReopened => LabelAwaitingFollowUp,
        _ => LabelAwaitingReply
    };

    public void MarkClosed()
    {
        IsHandled = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkReopened()
    {
        IsHandled = false;
        if (!HasReply)
        {
            UpdatedAt = DateTime.UtcNow;
            return;
        }

        var stamp = DateTime.UtcNow;
        if (AdminRepliedAt.HasValue && stamp <= AdminRepliedAt.Value)
        {
            stamp = AdminRepliedAt.Value.AddTicks(1);
        }

        UpdatedAt = stamp;
    }

    /// <summary>
    /// Saves the reply that support is sending now. An edit replaces the stored reply.
    /// </summary>
    public string? TrySaveReply(string reply, string adminUserId)
    {
        var normalized = (reply ?? string.Empty).Trim();
        if (normalized.Length == 0)
        {
            return "Reply cannot be empty.";
        }

        if (normalized.Length > 2000)
        {
            return "Reply cannot exceed 2 000 characters.";
        }

        var repliedAt = DateTime.UtcNow;
        AdminReply = normalized;
        AdminRepliedAt = repliedAt;
        AdminUserId = adminUserId;
        IsHandled = false;
        UpdatedAt = repliedAt;
        return null;
    }

    public string? TryAppendFollowUp(string followUp)
    {
        var addition = (followUp ?? string.Empty).Trim();
        if (addition.Length == 0)
        {
            return "Write a follow-up before sending.";
        }

        var existing = (Message ?? string.Empty).Trim();
        var stored = existing.Length == 0 ? addition : existing + "\n\n" + addition;
        if (stored.Length > 1000)
        {
            return "This conversation cannot fit another follow-up. Your earlier question and reply were left unchanged.";
        }

        Message = stored;
        if (IsHandled || (HasReply && !IsReopened))
        {
            MarkReopened();
        }
        else
        {
            UpdatedAt = DateTime.UtcNow;
        }

        return null;
    }
}
