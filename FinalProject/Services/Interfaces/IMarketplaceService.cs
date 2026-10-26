using FinalProject.DTOs;

namespace FinalProject.Services.Interfaces;

public interface IMarketplaceService
{
    Task<IReadOnlyList<FavoriteResponse>> GetFavoritesAsync(string userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<int>> GetFavoriteIdsAsync(string userId, CancellationToken cancellationToken = default);

    Task<FavoriteResponse?> AddFavoriteAsync(string userId, int carId, CancellationToken cancellationToken = default);

    Task<bool> RemoveFavoriteAsync(string userId, int carId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductFavoriteResponse>> GetProductFavoritesAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<int>> GetProductFavoriteIdsAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<ProductFavoriteResponse?> AddProductFavoriteAsync(
        string userId,
        int productId,
        CancellationToken cancellationToken = default);

    Task<bool> RemoveProductFavoriteAsync(
        string userId,
        int productId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ConversationSummaryResponse>> GetConversationsAsync(
        string userId,
        bool asStoreStaff = false,
        CancellationToken cancellationToken = default);

    Task<ConversationDetailsResponse?> GetConversationAsync(
        int id,
        string userId,
        bool markRead,
        bool asStoreStaff = false,
        CancellationToken cancellationToken = default);

    Task<ConversationDetailsResponse?> StartConversationAsync(
        int carId,
        string buyerId,
        StartConversationRequest request,
        CancellationToken cancellationToken = default);

    Task<MessageResponse?> ReplyAsync(
        int conversationId,
        string senderId,
        string content,
        bool asStoreStaff = false,
        CancellationToken cancellationToken = default);

    Task<bool> HideConversationAsync(
        int conversationId,
        string userId,
        bool asStoreStaff = false,
        CancellationToken cancellationToken = default);

    Task<AccountSummaryResponse> GetSummaryAsync(
        string userId,
        bool asStoreStaff = false,
        CancellationToken cancellationToken = default);
}
