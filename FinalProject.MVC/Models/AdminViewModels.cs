using FinalProject.MVC.DTOs;

namespace FinalProject.MVC.Models;

public sealed class AdminDashboardViewModel
{
    public AdminSummaryDto Summary { get; set; } = new();

    public int TotalMembers { get; set; }

    public int TotalSpareParts { get; set; }

    public int ActiveSpareParts { get; set; }

    public int NewContactRequests { get; set; }

    public int OpenProductQuestions { get; set; }

    public int OpenPurchaseRequests { get; set; }

    public int RecordedSales { get; set; }

    public int ActiveCampaigns { get; set; }

    public bool CatalogSummaryAvailable { get; set; }

    public bool ServiceUnavailable { get; set; }
}

public sealed class AdminUsersPageViewModel
{
    public string? Search { get; set; }

    /// <summary>When true, the list shows soft-deleted accounts only.</summary>
    public bool ShowDeleted { get; set; }

    public IReadOnlyList<AdminUserViewModel> Users { get; set; } = Array.Empty<AdminUserViewModel>();
}

public sealed class AdminUserViewModel
{
    public string Id { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    public bool EmailConfirmed { get; set; }

    public bool PhoneNumberConfirmed { get; set; }

    public string Role { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public bool IsLockedOut { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public int AccessFailedCount { get; set; }

    public bool IsSuperAdmin { get; set; }

    /// <summary>Account status for the Users table: Deleted, Locked, or Active.</summary>
    public string AccountStatus =>
        IsDeleted ? "Deleted" : IsLockedOut ? "Locked" : "Active";
}

public sealed class AdminUserRoleChangeItem
{
    public string UserId { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;
}

public sealed class AdminUserDetailsViewModel
{
    public string Id { get; set; } = string.Empty;

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string Email { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    public bool EmailConfirmed { get; set; }

    public bool PhoneNumberConfirmed { get; set; }

    public string Role { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime? LastLoginAt { get; set; }

    public DateTime? LastPasswordChangedAt { get; set; }

    public DateTime? LastPasswordResetAt { get; set; }

    public bool IsLockedOut { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public int AccessFailedCount { get; set; }

    public string AccountStatus =>
        IsDeleted ? "Deleted" : IsLockedOut ? "Locked" : "Active";

    public IReadOnlyList<AdminUserListingRow> Listings { get; set; } = Array.Empty<AdminUserListingRow>();

    public bool ListingsUnavailable { get; set; }

    public IReadOnlyList<AdminUserRequestRow> ContactRequests { get; set; } =
        Array.Empty<AdminUserRequestRow>();

    public IReadOnlyList<AdminUserRequestRow> PurchaseRequests { get; set; } =
        Array.Empty<AdminUserRequestRow>();
}

public sealed class AdminUserListingRow
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}

public sealed class AdminUserRequestRow
{
    public int Id { get; set; }

    public string Summary { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}

public sealed class LookupEditorViewModel
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public IReadOnlyList<BrandApiDto> Brands { get; set; } = Array.Empty<BrandApiDto>();

    public IReadOnlyList<CategoryApiDto> Categories { get; set; } = Array.Empty<CategoryApiDto>();
}
