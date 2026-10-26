using System.ComponentModel.DataAnnotations;
using FinalProject.MVC.Models.Api;

namespace FinalProject.MVC.Models;

public sealed class AccountDashboardViewModel
{
    public string DisplayName { get; set; } = string.Empty;

    public AccountSummaryDto Summary { get; set; } = new();
}

public sealed class AccountListingsViewModel
{
    public IReadOnlyList<CarApiDto> Cars { get; set; } = Array.Empty<CarApiDto>();
}

public sealed class AccountFavoritesViewModel
{
    public IReadOnlyList<FavoriteDto> Favorites { get; set; } = Array.Empty<FavoriteDto>();

    public IReadOnlyList<ProductFavoriteDto> ProductFavorites { get; set; } = Array.Empty<ProductFavoriteDto>();
}

public sealed class AccountMessagesViewModel
{
    public IReadOnlyList<ConversationSummaryDto> Conversations { get; set; } =
        Array.Empty<ConversationSummaryDto>();
}

public sealed class AccountProfileViewModel
{
    [MaxLength(100)]
    public string? FirstName { get; set; }

    [MaxLength(100)]
    public string? LastName { get; set; }

    public string Email { get; set; } = string.Empty;

    [Phone, MaxLength(50)]
    public string? PhoneNumber { get; set; }
}

public sealed class ChangePasswordViewModel
{
    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Current password")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 6)]
    [DataType(DataType.Password)]
    [Display(Name = "New password")]
    public string NewPassword { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm new password")]
    [Compare(nameof(NewPassword), ErrorMessage = "The new password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public sealed class ForgotPasswordViewModel
{
    [Required]
    [EmailAddress]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;
}

public sealed class ResetPasswordViewModel
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public string Token { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 6)]
    [DataType(DataType.Password)]
    [Display(Name = "New password")]
    public string Password { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    [Compare(nameof(Password), ErrorMessage = "The password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public sealed class CarEditorViewModel
{
    public int? Id { get; set; }

    public SaveCarDto Car { get; set; } = new();

    public CarLookupDto Lookups { get; set; } = new();

    public List<CarImageApiDto> Gallery { get; set; } = new();
}
