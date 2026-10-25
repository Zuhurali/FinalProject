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

public sealed class CarEditorViewModel
{
    public int? Id { get; set; }

    public SaveCarDto Car { get; set; } = new();

    public CarLookupDto Lookups { get; set; } = new();

    public List<CarImageApiDto> Gallery { get; set; } = new();
}
