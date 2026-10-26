namespace FinalProject.Models.Entities;

/// <summary>MVC-owned table; mapped for admin API read/write without owning migrations.</summary>
public class ShowroomPurchaseRequestReference
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public ShopProductReference? Product { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public int Quantity { get; set; } = 1;

    public DateTime? PreferredVisitDate { get; set; }

    public string? Note { get; set; }

    public Guid SubmissionKey { get; set; }

    public string Status { get; set; } = "New";

    public string? UserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
