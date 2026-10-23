using System.ComponentModel.DataAnnotations;

namespace FinalProject.MVC.Models;

public class ContactRequest
{
    public int Id { get; set; }

    [Required, MaxLength(80)]
    public string CustomerName { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required, MaxLength(40)]
    public string Phone { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string Interest { get; set; } = string.Empty;

    [Required, MaxLength(1000)]
    public string Message { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string Status { get; set; } = ContactRequestStatus.New;

    /// <summary>Identity user id when submitted while signed in; null for anonymous guests.</summary>
    [MaxLength(450)]
    public string? UserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}

public static class ContactRequestStatus
{
    public const string New = "New";
    public const string InProgress = "In Progress";
    public const string Resolved = "Resolved";

    public static readonly IReadOnlyList<string> All = [New, InProgress, Resolved];

    public static bool IsValid(string? status) =>
        !string.IsNullOrWhiteSpace(status)
        && All.Contains(status, StringComparer.Ordinal);
}
