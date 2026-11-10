using FinalProject.Models.Entities;

namespace FinalProject.Repositories;

public sealed record ProductFavoriteProductRow(
    int FavoriteId,
    DateTime CreatedAt,
    int ProductId,
    string Name,
    decimal Price,
    string Category,
    string Brand,
    string ImagePath,
    int Quantity,
    bool InStock);

public sealed record MarketplaceAccountActivityRow(
    string ActivityKey,
    string Type,
    string Description,
    DateTime CreatedAt,
    string? Url);

public sealed record MarketplaceAccountSummaryRow(
    int ActiveListings,
    int Favorites,
    int UnreadMessages,
    IReadOnlyList<MarketplaceAccountActivityRow> Activity);

public sealed record MarketplaceConversationStartResult(
    bool Success,
    Conversation? Conversation,
    CarMessage? InitialMessage);

public sealed record MarketplaceReplyPersistResult(
    bool Success,
    CarMessage? Message,
    IdentityUserReference? Sender);

public interface IMarketplaceRepository
{
    Task<string?> ResolveUserIdByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Favorite>> GetCarFavoritesForUserAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<int>> GetCarFavoriteIdsAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<bool> ActiveCarExistsAsync(int carId, CancellationToken cancellationToken = default);

    Task<Favorite?> FindCarFavoriteTrackedAsync(
        string userId,
        int carId,
        CancellationToken cancellationToken = default);

    Task<Favorite> AddOrGetCarFavoriteAsync(
        string userId,
        int carId,
        CancellationToken cancellationToken = default);

    Task<Favorite?> GetCarFavoriteWithCarAsync(int favoriteId, CancellationToken cancellationToken = default);

    Task<bool> RemoveCarFavoriteAsync(string userId, int carId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductFavoriteProductRow>> GetProductFavoritesForUserAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<int>> GetProductFavoriteIdsAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<ShopProductReference?> GetActiveProductAsync(
        int productId,
        CancellationToken cancellationToken = default);

    Task<ProductFavorite?> FindProductFavoriteTrackedAsync(
        string userId,
        int productId,
        CancellationToken cancellationToken = default);

    Task<ProductFavorite> AddOrGetProductFavoriteAsync(
        string userId,
        int productId,
        CancellationToken cancellationToken = default);

    Task<bool> RemoveProductFavoriteAsync(
        string userId,
        int productId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<int>> GetHiddenConversationIdsAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Conversation>> GetInboxConversationsAsync(
        string userId,
        IReadOnlyCollection<int> hiddenConversationIds,
        CancellationToken cancellationToken = default);

    Task<Conversation?> GetConversationForDetailsAsync(
        int conversationId,
        CancellationToken cancellationToken = default);

    Task<int> MarkConversationMessagesReadAsync(
        int conversationId,
        string userId,
        CancellationToken cancellationToken = default);

    Task<Car?> GetActiveCarWithBrandAsync(int carId, CancellationToken cancellationToken = default);

    Task<Conversation?> FindConversationByParticipantsAsync(
        int carId,
        string buyerId,
        string sellerId,
        CancellationToken cancellationToken = default);

    Task<MarketplaceConversationStartResult> PersistConversationStartAsync(
        int carId,
        string buyerId,
        string sellerId,
        string subject,
        string content,
        CancellationToken cancellationToken = default);

    Task<Conversation?> GetConversationTrackedAsync(
        int conversationId,
        CancellationToken cancellationToken = default);

    Task<MarketplaceReplyPersistResult> PersistReplyAsync(
        int conversationId,
        string senderId,
        string receiverId,
        string content,
        CancellationToken cancellationToken = default);

    Task<Conversation?> GetConversationForAccessCheckAsync(
        int conversationId,
        CancellationToken cancellationToken = default);

    Task<bool> HideConversationForUserAsync(
        int conversationId,
        string userId,
        CancellationToken cancellationToken = default);

    Task<MarketplaceAccountSummaryRow> GetAccountSummaryAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<IdentityUserReference> GetIdentityUserAsync(
        string userId,
        CancellationToken cancellationToken = default);
}
