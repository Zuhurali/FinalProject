using System.ComponentModel.DataAnnotations;

namespace FinalProject.MVC.Models;

public class ShowroomPurchaseRequest
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public ShopCatalogProduct? Product { get; set; }

    [Required, MaxLength(80)]
    public string CustomerName { get; set; } = string.Empty;

    [Required, MaxLength(200), EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, MaxLength(40)]
    public string Phone { get; set; } = string.Empty;

    public int Quantity { get; set; } = 1;

    public DateTime? PreferredVisitDate { get; set; }

    [MaxLength(500)]
    public string? Note { get; set; }

    public Guid SubmissionKey { get; set; }

    [MaxLength(20)]
    public string Status { get; set; } = ShowroomRequestStatus.New;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}

public static class ShowroomRequestStatus
{
    public const string New = "New";
    public const string Contacted = "Contacted";
    public const string Completed = "Completed";
    public const string Cancelled = "Cancelled";

    public static readonly IReadOnlyList<string> All = [New, Contacted, Completed, Cancelled];

    public static bool IsOpen(string status) => status is New or Contacted;
}
