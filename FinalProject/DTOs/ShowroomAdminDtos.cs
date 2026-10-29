using System.ComponentModel.DataAnnotations;

namespace FinalProject.DTOs;

public sealed class ShowroomRequestItem
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public string? ProductName { get; set; }

    public int? StockNow { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public DateTime? PreferredVisitDate { get; set; }

    public string? Note { get; set; }

    public string Status { get; set; } = string.Empty;

    public bool HasLinkedAccount { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public IReadOnlyList<string> AllowedNextStatuses { get; set; } = Array.Empty<string>();
}

public sealed class UpdateShowroomRequestStatusRequest
{
    [Required, MaxLength(20)]
    public string Status { get; set; } = string.Empty;
}

public sealed class RecordShowroomSaleRequest
{
    [Range(1, int.MaxValue)]
    public int ProductId { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; } = 1;

    [MaxLength(500)]
    public string? Note { get; set; }

    /// <summary>Client-generated key; retries with the same key return the original sale.</summary>
    public Guid? IdempotencyKey { get; set; }
}

public sealed class CompleteShowroomRequestSaleRequest
{
    [MaxLength(500)]
    public string? Note { get; set; }
}

public sealed class ShowroomSaleItem
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public string? ProductName { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal ListUnitPrice { get; set; }

    public int? CampaignId { get; set; }

    public decimal? CampaignDiscountPercent { get; set; }

    public string Note { get; set; } = string.Empty;

    public DateTime SoldAt { get; set; }

    public string RecordedByUserId { get; set; } = string.Empty;
}
