using FinalProject.DTOs;

namespace FinalProject.Services.Interfaces;

public interface IMarketplaceService
{
    Task<IReadOnlyList<FavoriteResponse>> GetFavoritesAsync(string userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<int>> GetFavoriteIdsAsync(string userId, CancellationToken cancellationToken = default);

    Task<FavoriteResponse?> AddFavoriteAsync(string userId, int carId, CancellationToken cancellationToken = default);

    Task<bool> RemoveFavoriteAsync(string userId, int carId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ConversationSummaryResponse>> GetConversationsAsync(string userId, CancellationToken cancellationToken = default);

    Task<ConversationDetailsResponse?> GetConversationAsync(int id, string userId, bool markRead, CancellationToken cancellationToken = default);

    Task<ConversationDetailsResponse?> StartConversationAsync(
        int carId,
        string buyerId,
        StartConversationRequest request,
        CancellationToken cancellationToken = default);

    Task<MessageResponse?> ReplyAsync(
        int conversationId,
        string senderId,
        string content,
        CancellationToken cancellationToken = default);

    Task<AccountSummaryResponse> GetSummaryAsync(string userId, CancellationToken cancellationToken = default);
}
