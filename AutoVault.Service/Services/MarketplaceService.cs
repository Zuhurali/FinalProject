using FinalProject.Commerce;
using FinalProject.Repositories;
using FinalProject.DTOs;
using FinalProject.Mappings;
using FinalProject.Models.Entities;
using FinalProject.Services.Interfaces;
using Microsoft.Extensions.Configuration;

namespace FinalProject.Services;

public sealed class MarketplaceService : IMarketplaceService
{
    private readonly IMarketplaceRepository _context;
    private readonly IProductRatingRepository _users;
    private readonly IConfiguration _configuration;
    private readonly INotificationService _notifications;

    public MarketplaceService(
        IMarketplaceRepository context,
        IProductRatingRepository users,
        IConfiguration configuration,
        INotificationService notifications)
    {
        _context = context;
        _users = users;
        _configuration = configuration;
        _notifications = notifications;
    }

    public async Task<IReadOnlyList<FavoriteResponseDto>> GetFavoritesAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var favorites = await _context.GetCarFavoritesForUserAsync(userId, cancellationToken);
        return favorites
            .Select(favorite => new FavoriteResponseDto(
                favorite.Id,
                favorite.CreatedAt,
                favorite.Car.ToResponse()))
            .ToList();
    }

    public Task<IReadOnlyList<int>> GetFavoriteIdsAsync(
        string userId,
        CancellationToken cancellationToken = default) =>
        _context.GetCarFavoriteIdsAsync(userId, cancellationToken);

    public async Task<FavoriteResponseDto?> AddFavoriteAsync(
        string userId,
        int carId,
        CancellationToken cancellationToken = default)
    {
        if (!await _users.UserExistsAsync(userId, cancellationToken)
            || !await _context.ActiveCarExistsAsync(carId, cancellationToken))
        {
            return null;
        }

        var existing = await _context.AddOrGetCarFavoriteAsync(userId, carId, cancellationToken);
        var favorite = await _context.GetCarFavoriteWithCarAsync(existing.Id, cancellationToken);
        if (favorite == null)
        {
            return null;
        }

        return new FavoriteResponseDto(
            favorite.Id,
            favorite.CreatedAt,
            favorite.Car.ToResponse());
    }

    public async Task<IReadOnlyList<ProductFavoriteResponseDto>> GetProductFavoritesAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var favorites = await _context.GetProductFavoritesForUserAsync(userId, cancellationToken);
        return favorites.Select(MapProductFavorite).ToList();
    }

    public Task<IReadOnlyList<int>> GetProductFavoriteIdsAsync(
        string userId,
        CancellationToken cancellationToken = default) =>
        _context.GetProductFavoriteIdsAsync(userId, cancellationToken);

    public async Task<ProductFavoriteResponseDto?> AddProductFavoriteAsync(
        string userId,
        int productId,
        CancellationToken cancellationToken = default)
    {
        if (!await _users.UserExistsAsync(userId, cancellationToken))
        {
            return null;
        }

        var product = await _context.GetActiveProductAsync(productId, cancellationToken);
        if (product == null)
        {
            return null;
        }

        var existing = await _context.AddOrGetProductFavoriteAsync(userId, productId, cancellationToken);
        return MapProductFavorite(
            existing.Id,
            existing.CreatedAt,
            product);
    }

    public Task<bool> RemoveProductFavoriteAsync(
        string userId,
        int productId,
        CancellationToken cancellationToken = default) =>
        _context.RemoveProductFavoriteAsync(userId, productId, cancellationToken);

    public Task<bool> RemoveFavoriteAsync(
        string userId,
        int carId,
        CancellationToken cancellationToken = default) =>
        _context.RemoveCarFavoriteAsync(userId, carId, cancellationToken);

    public async Task<IReadOnlyList<ConversationSummaryResponseDto>> GetConversationsAsync(
        string userId,
        bool asStoreStaff = false,
        CancellationToken cancellationToken = default)
    {
        _ = asStoreStaff;

        var hiddenIds = await _context.GetHiddenConversationIdsAsync(userId, cancellationToken);
        var conversations = await _context.GetInboxConversationsAsync(userId, hiddenIds, cancellationToken);

        return conversations.Select(conversation =>
        {
            var viewingAsSeller = conversation.SellerId == userId;
            var other = viewingAsSeller
                ? conversation.Buyer
                : conversation.Seller;
            var lastMessage = conversation.Messages
                .OrderByDescending(message => message.CreatedAt)
                .FirstOrDefault();

            return new ConversationSummaryResponseDto(
                conversation.Id,
                conversation.CarId,
                CarName(conversation.Car),
                MainImage(conversation.Car),
                DisplayName(other),
                MarketplaceMessagingRules.StripGeneratedListingText(lastMessage?.Content),
                conversation.UpdatedAt ?? conversation.CreatedAt,
                conversation.Messages.Count(message =>
                    message.ReceiverId == userId && !message.IsRead));
        }).ToList();
    }

    public async Task<ConversationDetailsResponseDto?> GetConversationAsync(
        int id,
        string userId,
        bool markRead,
        bool asStoreStaff = false,
        CancellationToken cancellationToken = default)
    {
        var conversation = await _context.GetConversationForDetailsAsync(id, cancellationToken);
        if (conversation == null
            || !CanAccessConversation(conversation, userId))
        {
            return null;
        }

        _ = asStoreStaff;

        if (markRead)
        {
            await _context.MarkConversationMessagesReadAsync(id, userId, cancellationToken);
            conversation = await _context.GetConversationForDetailsAsync(id, cancellationToken);
            if (conversation == null)
            {
                return null;
            }
        }

        return ToDetails(conversation);
    }

    public async Task<ConversationDetailsResponseDto?> StartConversationAsync(
        int carId,
        string buyerId,
        StartConversationRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var car = await _context.GetActiveCarWithBrandAsync(carId, cancellationToken);
        if (car == null || !await _users.UserExistsAsync(buyerId, cancellationToken))
        {
            return null;
        }

        var sellerId = await ResolveSellerIdAsync(car, cancellationToken);
        if (sellerId == null || sellerId == buyerId)
        {
            return null;
        }

        var carName = CarName(car);
        var subject = string.IsNullOrWhiteSpace(request.Subject)
            ? MarketplaceMessagingRules.DefaultSubject(carName)
            : request.Subject.Trim();
        var content = MarketplaceMessagingRules.StripGeneratedListingText(request.Content);
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        var persisted = await _context.PersistConversationStartAsync(
            carId,
            buyerId,
            sellerId,
            subject,
            content,
            cancellationToken);
        if (!persisted.Success || persisted.Conversation == null || persisted.InitialMessage == null)
        {
            return null;
        }

        var buyer = await _context.GetIdentityUserAsync(buyerId, cancellationToken);
        var notification = await _notifications.CreateMessageNotificationAsync(
            sellerId,
            DisplayName(buyer),
            persisted.Conversation.Id,
            persisted.InitialMessage.Id,
            content,
            cancellationToken);

        var details = await GetConversationAsync(
            persisted.Conversation.Id,
            buyerId,
            markRead: false,
            asStoreStaff: false,
            cancellationToken);
        return details == null
            ? null
            : details with { Notification = notification };
    }

    public async Task<MessageResponseDto?> ReplyAsync(
        int conversationId,
        string senderId,
        string content,
        bool asStoreStaff = false,
        CancellationToken cancellationToken = default)
    {
        _ = asStoreStaff;

        var conversation = await _context.GetConversationForAccessCheckAsync(conversationId, cancellationToken);
        if (conversation == null || !CanAccessConversation(conversation, senderId))
        {
            return null;
        }

        var receiverId = conversation.SellerId == senderId
            ? conversation.BuyerId
            : conversation.SellerId;
        var cleaned = MarketplaceMessagingRules.StripGeneratedListingText(content);
        if (string.IsNullOrWhiteSpace(cleaned))
        {
            return null;
        }

        var persisted = await _context.PersistReplyAsync(
            conversationId,
            senderId,
            receiverId,
            cleaned,
            cancellationToken);
        if (!persisted.Success || persisted.Message == null || persisted.Sender == null)
        {
            return null;
        }

        var message = persisted.Message;
        var senderDisplayName = DisplayName(persisted.Sender);
        var notification = senderId == receiverId
            ? null
            : await _notifications.CreateMessageNotificationAsync(
                receiverId,
                senderDisplayName,
                conversationId,
                message.Id,
                message.Content,
                cancellationToken);

        return new MessageResponseDto(
            message.Id,
            message.SenderId,
            senderDisplayName,
            message.ReceiverId,
            message.Content,
            message.IsRead,
            message.CreatedAt,
            notification);
    }

    public async Task<bool> HideConversationAsync(
        int conversationId,
        string userId,
        bool asStoreStaff = false,
        CancellationToken cancellationToken = default)
    {
        _ = asStoreStaff;

        var conversation = await _context.GetConversationForAccessCheckAsync(conversationId, cancellationToken);
        if (conversation == null || !CanAccessConversation(conversation, userId))
        {
            return false;
        }

        if (!ConversationHideRules.CanHideForUser(
                userId,
                conversation.BuyerId,
                conversation.SellerId))
        {
            return false;
        }

        return await _context.HideConversationForUserAsync(conversationId, userId, cancellationToken);
    }

    public async Task<AccountSummaryResponseDto> GetSummaryAsync(
        string userId,
        bool asStoreStaff = false,
        CancellationToken cancellationToken = default)
    {
        _ = asStoreStaff;
        var summary = await _context.GetAccountSummaryAsync(userId, cancellationToken);
        return new AccountSummaryResponseDto(
            summary.ActiveListings,
            summary.Favorites,
            summary.UnreadMessages,
            summary.Activity
                .Select(item => new AccountActivityResponseDto(
                    item.ActivityKey,
                    item.Type,
                    item.Description,
                    item.CreatedAt,
                    item.Url))
                .ToList());
    }

    private static ProductFavoriteResponseDto MapProductFavorite(ProductFavoriteProductRow row) =>
        new(
            row.FavoriteId,
            row.CreatedAt,
            row.ProductId,
            row.Name,
            row.Price,
            row.Category,
            row.Brand,
            row.ImagePath,
            row.Quantity,
            row.InStock);

    private static ProductFavoriteResponseDto MapProductFavorite(
        int id,
        DateTime createdAt,
        ShopProductReference product) =>
        new(
            id,
            createdAt,
            product.Id,
            product.Name,
            product.Price,
            product.Category,
            product.Brand,
            product.ImagePath,
            product.Quantity,
            product.InStock);

    private async Task<string?> ResolveSellerIdAsync(
        Car car,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(car.OwnerId)
            && await _users.UserExistsAsync(car.OwnerId, cancellationToken))
        {
            return car.OwnerId;
        }

        return await ResolveStoreInboxUserIdAsync(cancellationToken);
    }

    private async Task<string?> ResolveStoreInboxUserIdAsync(CancellationToken cancellationToken)
    {
        var inboxEmail = _configuration["Marketplace:StoreInboxEmail"]
            ?? _configuration["AdminSeed:SuperAdminEmail"];
        if (string.IsNullOrWhiteSpace(inboxEmail))
        {
            return null;
        }

        return await _context.ResolveUserIdByEmailAsync(inboxEmail, cancellationToken);
    }

    private static bool CanAccessConversation(Conversation conversation, string userId) =>
        conversation.BuyerId == userId || conversation.SellerId == userId;

    private static ConversationDetailsResponseDto ToDetails(Conversation conversation)
    {
        return new ConversationDetailsResponseDto(
            conversation.Id,
            conversation.CarId,
            CarName(conversation.Car),
            MainImage(conversation.Car),
            conversation.BuyerId,
            DisplayName(conversation.Buyer),
            conversation.SellerId,
            DisplayName(conversation.Seller),
            conversation.Subject,
            conversation.Messages
                .OrderBy(message => message.CreatedAt)
                .Select(message => new MessageResponseDto(
                    message.Id,
                    message.SenderId,
                    DisplayName(message.Sender),
                    message.ReceiverId,
                    MarketplaceMessagingRules.StripGeneratedListingText(message.Content),
                    message.IsRead,
                    message.CreatedAt))
                .ToList());
    }

    private static string CarName(Car car) => $"{car.Brand.Name} {car.Model}".Trim();

    private static string? MainImage(Car car) =>
        car.Images.OrderBy(image => image.DisplayOrder).FirstOrDefault(image => image.IsMain)?.ImageUrl
        ?? car.Images.OrderBy(image => image.DisplayOrder).FirstOrDefault()?.ImageUrl;

    private static string DisplayName(IdentityUserReference user)
    {
        var fullName = $"{user.FirstName} {user.LastName}".Trim();
        return string.IsNullOrWhiteSpace(fullName)
            ? user.Email ?? user.UserName ?? "User"
            : fullName;
    }
}
