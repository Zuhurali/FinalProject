using FinalProject.DTOs;

namespace FinalProject.Services.Interfaces;

public interface IMarketplaceService
{
    Task<IReadOnlyList<FavoriteResponseDto>> GetFavoritesAsync(string userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<int>> GetFavoriteIdsAsync(string userId, CancellationToken cancellationToken = default);

    Task<FavoriteResponseDto?> AddFavoriteAsync(string userId, int carId, CancellationToken cancellationToken = default);

    Task<bool> RemoveFavoriteAsync(string userId, int carId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductFavoriteResponseDto>> GetProductFavoritesAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<int>> GetProductFavoriteIdsAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<ProductFavoriteResponseDto?> AddProductFavoriteAsync(
        string userId,
        int productId,
        CancellationToken cancellationToken = default);

    Task<bool> RemoveProductFavoriteAsync(
        string userId,
        int productId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ConversationSummaryResponseDto>> GetConversationsAsync(
        string userId,
        bool asStoreStaff = false,
        CancellationToken cancellationToken = default);

    Task<ConversationDetailsResponseDto?> GetConversationAsync(
        int id,
        string userId,
        bool markRead,
        bool asStoreStaff = false,
        CancellationToken cancellationToken = default);

    Task<ConversationDetailsResponseDto?> StartConversationAsync(
        int carId,
        string buyerId,
        StartConversationRequestDto request,
        CancellationToken cancellationToken = default);

    Task<MessageResponseDto?> ReplyAsync(
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

    Task<AccountSummaryResponseDto> GetSummaryAsync(
        string userId,
        bool asStoreStaff = false,
        CancellationToken cancellationToken = default);
}
