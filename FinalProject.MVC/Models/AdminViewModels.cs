using FinalProject.MVC.Models.Api;

namespace FinalProject.MVC.Models;

public sealed class AdminDashboardViewModel
{
    public AdminSummaryDto Summary { get; set; } = new();

    public int TotalMembers { get; set; }

    public int TotalProducts { get; set; }

    public bool ServiceUnavailable { get; set; }
}

public sealed class AdminUserViewModel
{
    public string Id { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public bool EmailConfirmed { get; set; }

    public string Role { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public bool IsSuperAdmin { get; set; }
}

public sealed class AdminUserRoleChangeItem
{
    public string UserId { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;
}

public sealed class LookupEditorViewModel
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public IReadOnlyList<BrandApiDto> Brands { get; set; } = Array.Empty<BrandApiDto>();

    public IReadOnlyList<CategoryApiDto> Categories { get; set; } = Array.Empty<CategoryApiDto>();
}
