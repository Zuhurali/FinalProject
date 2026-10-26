using FinalProject.MVC.Models.Api;

namespace FinalProject.MVC.Services.Interfaces;

public interface IMarketplaceApiService
{
    Task<AccountSummaryDto> GetSummaryAsync(string accessToken, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FavoriteDto>> GetFavoritesAsync(string accessToken, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<int>> GetFavoriteIdsAsync(string accessToken, CancellationToken cancellationToken = default);

    Task AddFavoriteAsync(int carId, string accessToken, CancellationToken cancellationToken = default);

    Task RemoveFavoriteAsync(int carId, string accessToken, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductFavoriteDto>> GetProductFavoritesAsync(
        string accessToken,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<int>> GetProductFavoriteIdsAsync(
        string accessToken,
        CancellationToken cancellationToken = default);

    Task AddProductFavoriteAsync(int productId, string accessToken, CancellationToken cancellationToken = default);

    Task RemoveProductFavoriteAsync(int productId, string accessToken, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ConversationSummaryDto>> GetConversationsAsync(string accessToken, CancellationToken cancellationToken = default);

    Task<ConversationDetailsDto?> GetConversationAsync(int id, string accessToken, CancellationToken cancellationToken = default);

    Task<ConversationDetailsDto> StartConversationAsync(
        int carId,
        string? subject,
        string content,
        string accessToken,
        CancellationToken cancellationToken = default);

    Task<MessageDto> ReplyAsync(
        int conversationId,
        string content,
        string accessToken,
        CancellationToken cancellationToken = default);

    Task HideConversationAsync(
        int conversationId,
        string accessToken,
        CancellationToken cancellationToken = default);
}
