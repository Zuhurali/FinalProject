using FinalProject.Commerce;
using FinalProject.Data;
using FinalProject.DTOs;
using FinalProject.Mappings;
using FinalProject.Models.Entities;
using FinalProject.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace FinalProject.Services;

public sealed class MarketplaceService : IMarketplaceService
{
    private readonly FinalProjectDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly INotificationService _notifications;

    public MarketplaceService(
        FinalProjectDbContext context,
        IConfiguration configuration,
        INotificationService notifications)
    {
        _context = context;
        _configuration = configuration;
        _notifications = notifications;
    }

    public async Task<IReadOnlyList<FavoriteResponse>> GetFavoritesAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var favorites = await _context.Favorites
            .AsNoTracking()
            .Where(favorite =>
                favorite.UserId == userId
                && favorite.Car.Status.ToLower() == CarPublicListing.ActiveStatus.ToLower())
            .OrderByDescending(favorite => favorite.CreatedAt)
            .Include(favorite => favorite.Car.Brand)
            .Include(favorite => favorite.Car.Category)
            .Include(favorite => favorite.Car.Owner)
            .Include(favorite => favorite.Car.Images)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        return favorites
            .Select(favorite => new FavoriteResponse(
                favorite.Id,
                favorite.CreatedAt,
                favorite.Car.ToResponse()))
            .ToList();
    }

    public async Task<IReadOnlyList<int>> GetFavoriteIdsAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Favorites
            .AsNoTracking()
            .Where(favorite =>
                favorite.UserId == userId
                && favorite.Car.Status.ToLower() == CarPublicListing.ActiveStatus.ToLower())
            .OrderBy(favorite => favorite.CarId)
            .Select(favorite => favorite.CarId)
            .ToListAsync(cancellationToken);
    }

    public async Task<FavoriteResponse?> AddFavoriteAsync(
        string userId,
        int carId,
        CancellationToken cancellationToken = default)
    {
        if (!await UserExistsAsync(userId, cancellationToken)
            || !await _context.Cars.AnyAsync(
                car => car.Id == carId && car.Status.ToLower() == CarPublicListing.ActiveStatus.ToLower(),
                cancellationToken))
        {
            return null;
        }

        var existing = await _context.Favorites
            .SingleOrDefaultAsync(
                favorite => favorite.UserId == userId && favorite.CarId == carId,
                cancellationToken);
        if (existing == null)
        {
            existing = new Favorite
            {
                UserId = userId,
                CarId = carId,
                CreatedAt = DateTime.UtcNow
            };
            _context.Favorites.Add(existing);
            await _context.SaveChangesAsync(cancellationToken);
        }

        var favorite = await _context.Favorites
            .AsNoTracking()
            .Include(item => item.Car.Brand)
            .Include(item => item.Car.Category)
            .Include(item => item.Car.Owner)
            .Include(item => item.Car.Images)
            .AsSplitQuery()
            .SingleAsync(item => item.Id == existing.Id, cancellationToken);

        return new FavoriteResponse(
            favorite.Id,
            favorite.CreatedAt,
            favorite.Car.ToResponse());
    }

    public async Task<IReadOnlyList<ProductFavoriteResponse>> GetProductFavoritesAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var favorites = await _context.ProductFavorites
            .AsNoTracking()
            .Where(favorite => favorite.UserId == userId)
            .OrderByDescending(favorite => favorite.CreatedAt)
            .Join(
                _context.ShopProducts.AsNoTracking().Where(product => product.IsActive),
                favorite => favorite.ProductId,
                product => product.Id,
                (favorite, product) => new { favorite, product })
            .ToListAsync(cancellationToken);

        return favorites
            .Select(item => MapProductFavorite(item.favorite.Id, item.favorite.CreatedAt, item.product))
            .ToList();
    }

    public async Task<IReadOnlyList<int>> GetProductFavoriteIdsAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.ProductFavorites
            .AsNoTracking()
            .Where(favorite => favorite.UserId == userId)
            .Join(
                _context.ShopProducts.AsNoTracking().Where(product => product.IsActive),
                favorite => favorite.ProductId,
                product => product.Id,
                (favorite, product) => product.Id)
            .OrderBy(id => id)
            .ToListAsync(cancellationToken);
    }

    public async Task<ProductFavoriteResponse?> AddProductFavoriteAsync(
        string userId,
        int productId,
        CancellationToken cancellationToken = default)
    {
        if (!await UserExistsAsync(userId, cancellationToken))
        {
            return null;
        }

        var product = await _context.ShopProducts.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == productId && item.IsActive, cancellationToken);
        if (product == null)
        {
            return null;
        }

        var existing = await _context.ProductFavorites
            .SingleOrDefaultAsync(
                favorite => favorite.UserId == userId && favorite.ProductId == productId,
                cancellationToken);
        if (existing == null)
        {
            existing = new ProductFavorite
            {
                UserId = userId,
                ProductId = productId,
                CreatedAt = DateTime.UtcNow
            };
            _context.ProductFavorites.Add(existing);
            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                _context.ChangeTracker.Clear();
                existing = await _context.ProductFavorites
                    .SingleOrDefaultAsync(
                        favorite => favorite.UserId == userId && favorite.ProductId == productId,
                        cancellationToken);
                if (existing == null)
                {
                    throw;
                }
            }
        }

        return MapProductFavorite(existing.Id, existing.CreatedAt, product);
    }

    public async Task<bool> RemoveProductFavoriteAsync(
        string userId,
        int productId,
        CancellationToken cancellationToken = default)
    {
        var favorite = await _context.ProductFavorites
            .SingleOrDefaultAsync(
                item => item.UserId == userId && item.ProductId == productId,
                cancellationToken);
        if (favorite == null)
        {
            return false;
        }

        _context.ProductFavorites.Remove(favorite);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> RemoveFavoriteAsync(
        string userId,
        int carId,
        CancellationToken cancellationToken = default)
    {
        var favorite = await _context.Favorites
            .SingleOrDefaultAsync(
                item => item.UserId == userId && item.CarId == carId,
                cancellationToken);
        if (favorite == null)
        {
            return false;
        }

        _context.Favorites.Remove(favorite);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<ConversationSummaryResponse>> GetConversationsAsync(
        string userId,
        bool asStoreStaff = false,
        CancellationToken cancellationToken = default)
    {
        var storeInboxId = asStoreStaff
            ? await ResolveStoreInboxUserIdAsync(cancellationToken)
            : null;

        var hiddenIds = await _context.ConversationHides
            .AsNoTracking()
            .Where(hide => hide.UserId == userId)
            .Select(hide => hide.ConversationId)
            .ToListAsync(cancellationToken);

        var conversations = await _context.Conversations
            .AsNoTracking()
            .Where(conversation =>
                (conversation.BuyerId == userId
                    || conversation.SellerId == userId
                    || (storeInboxId != null && conversation.SellerId == storeInboxId))
                && !hiddenIds.Contains(conversation.Id))
            .OrderByDescending(conversation => conversation.UpdatedAt)
            .Include(conversation => conversation.Car.Brand)
            .Include(conversation => conversation.Car.Images)
            .Include(conversation => conversation.Buyer)
            .Include(conversation => conversation.Seller)
            .Include(conversation => conversation.Messages)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        return conversations.Select(conversation =>
        {
            var viewingAsSeller = conversation.SellerId == userId
                || (storeInboxId != null && conversation.SellerId == storeInboxId);
            var other = viewingAsSeller
                ? conversation.Buyer
                : conversation.Seller;
            var lastMessage = conversation.Messages
                .OrderByDescending(message => message.CreatedAt)
                .FirstOrDefault();
            var unreadReceiverIds = viewingAsSeller
                ? new[] { conversation.SellerId, userId }.Distinct().ToArray()
                : new[] { userId };

            return new ConversationSummaryResponse(
                conversation.Id,
                conversation.CarId,
                CarName(conversation.Car),
                MainImage(conversation.Car),
                DisplayName(other),
                MarketplaceMessagingRules.StripGeneratedListingText(lastMessage?.Content),
                conversation.UpdatedAt,
                conversation.Messages.Count(message =>
                    unreadReceiverIds.Contains(message.ReceiverId) && !message.IsRead));
        }).ToList();
    }

    public async Task<ConversationDetailsResponse?> GetConversationAsync(
        int id,
        string userId,
        bool markRead,
        bool asStoreStaff = false,
        CancellationToken cancellationToken = default)
    {
        var conversation = await _context.Conversations
            .Include(item => item.Car.Brand)
            .Include(item => item.Car.Images)
            .Include(item => item.Buyer)
            .Include(item => item.Seller)
            .Include(item => item.Messages)
                .ThenInclude(message => message.Sender)
            .AsSplitQuery()
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (conversation == null
            || !await CanAccessConversationAsync(
                conversation,
                userId,
                asStoreStaff,
                cancellationToken))
        {
            return null;
        }

        if (markRead)
        {
            var storeInboxId = asStoreStaff
                ? await ResolveStoreInboxUserIdAsync(cancellationToken)
                : null;
            var viewingAsSeller = conversation.SellerId == userId
                || (storeInboxId != null && conversation.SellerId == storeInboxId);
            var receiverIds = viewingAsSeller
                ? new[] { conversation.SellerId, userId }.Distinct().ToArray()
                : new[] { userId };

            var unread = conversation.Messages
                .Where(message => receiverIds.Contains(message.ReceiverId) && !message.IsRead)
                .ToList();
            if (unread.Count > 0)
            {
                unread.ForEach(message => message.IsRead = true);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        return ToDetails(conversation);
    }

    public async Task<ConversationDetailsResponse?> StartConversationAsync(
        int carId,
        string buyerId,
        StartConversationRequest request,
        CancellationToken cancellationToken = default)
    {
        var car = await _context.Cars
            .AsNoTracking()
            .Include(item => item.Brand)
            .SingleOrDefaultAsync(
                item => item.Id == carId && item.Status.ToLower() == CarPublicListing.ActiveStatus.ToLower(),
                cancellationToken);
        if (car == null || !await UserExistsAsync(buyerId, cancellationToken))
        {
            return null;
        }

        var sellerId = await ResolveSellerIdAsync(car, cancellationToken);
        if (sellerId == null || sellerId == buyerId)
        {
            return null;
        }

        var conversation = await _context.Conversations
            .SingleOrDefaultAsync(item =>
                item.CarId == carId
                && item.BuyerId == buyerId
                && item.SellerId == sellerId,
                cancellationToken);
        var now = DateTime.UtcNow;
        var carName = CarName(car);
        var subject = string.IsNullOrWhiteSpace(request.Subject)
            ? MarketplaceMessagingRules.DefaultSubject(carName)
            : request.Subject.Trim();
        var content = MarketplaceMessagingRules.StripGeneratedListingText(request.Content);
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        if (conversation == null)
        {
            conversation = new Conversation
            {
                CarId = carId,
                BuyerId = buyerId,
                SellerId = sellerId,
                Subject = subject,
                CreatedAt = now,
                UpdatedAt = now
            };
            _context.Conversations.Add(conversation);
        }
        else
        {
            conversation.UpdatedAt = now;
            if (string.IsNullOrWhiteSpace(conversation.Subject))
            {
                conversation.Subject = subject;
            }
        }

        var message = new CarMessage
        {
            SenderId = buyerId,
            ReceiverId = sellerId,
            Content = content,
            CreatedAt = now
        };
        conversation.Messages.Add(message);

        await ClearConversationHidesAsync(conversation.Id, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        var buyer = await _context.IdentityUsers
            .AsNoTracking()
            .SingleAsync(user => user.Id == buyerId, cancellationToken);
        var notification = await _notifications.CreateMessageNotificationAsync(
            sellerId,
            DisplayName(buyer),
            conversation.Id,
            message.Id,
            content,
            cancellationToken);

        var details = await GetConversationAsync(
            conversation.Id,
            buyerId,
            markRead: false,
            asStoreStaff: false,
            cancellationToken);
        return details == null
            ? null
            : details with { Notification = notification };
    }

    public async Task<MessageResponse?> ReplyAsync(
        int conversationId,
        string senderId,
        string content,
        bool asStoreStaff = false,
        CancellationToken cancellationToken = default)
    {
        var conversation = await _context.Conversations
            .SingleOrDefaultAsync(item => item.Id == conversationId, cancellationToken);
        if (conversation == null
            || !await CanAccessConversationAsync(
                conversation,
                senderId,
                asStoreStaff,
                cancellationToken))
        {
            return null;
        }

        var storeInboxId = asStoreStaff
            ? await ResolveStoreInboxUserIdAsync(cancellationToken)
            : null;
        var viewingAsSeller = conversation.SellerId == senderId
            || (storeInboxId != null && conversation.SellerId == storeInboxId);
        var receiverId = viewingAsSeller
            ? conversation.BuyerId
            : conversation.SellerId;
        var cleaned = MarketplaceMessagingRules.StripGeneratedListingText(content);
        if (string.IsNullOrWhiteSpace(cleaned))
        {
            return null;
        }

        var now = DateTime.UtcNow;
        var message = new CarMessage
        {
            ConversationId = conversationId,
            SenderId = senderId,
            ReceiverId = receiverId,
            Content = cleaned,
            CreatedAt = now
        };
        conversation.UpdatedAt = now;
        _context.Messages.Add(message);
        await ClearConversationHidesAsync(conversationId, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        var sender = await _context.IdentityUsers
            .AsNoTracking()
            .SingleAsync(user => user.Id == senderId, cancellationToken);
        var senderDisplayName = DisplayName(sender);
        var notification = senderId == receiverId
            ? null
            : await _notifications.CreateMessageNotificationAsync(
                receiverId,
                senderDisplayName,
                conversationId,
                message.Id,
                message.Content,
                cancellationToken);

        return new MessageResponse(
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
        var conversation = await _context.Conversations
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == conversationId, cancellationToken);
        if (conversation == null
            || !await CanAccessConversationAsync(
                conversation,
                userId,
                asStoreStaff,
                cancellationToken))
        {
            return false;
        }

        var storeInboxId = asStoreStaff
            ? await ResolveStoreInboxUserIdAsync(cancellationToken)
            : null;
        if (!ConversationHideRules.CanHideForUser(
                userId,
                conversation.BuyerId,
                conversation.SellerId,
                asStoreStaff,
                storeInboxId))
        {
            return false;
        }

        var existing = await _context.ConversationHides
            .SingleOrDefaultAsync(
                hide => hide.ConversationId == conversationId && hide.UserId == userId,
                cancellationToken);
        if (existing != null)
        {
            return true;
        }

        _context.ConversationHides.Add(new ConversationHide
        {
            ConversationId = conversationId,
            UserId = userId,
            HiddenAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<AccountSummaryResponse> GetSummaryAsync(
        string userId,
        bool asStoreStaff = false,
        CancellationToken cancellationToken = default)
    {
        var activeListings = await _context.Cars
            .CountAsync(
                car => car.OwnerId == userId && car.Status == "Active",
                cancellationToken);
        var carFavorites = await _context.Favorites
            .CountAsync(
                favorite =>
                    favorite.UserId == userId
                    && favorite.Car.Status.ToLower() == CarPublicListing.ActiveStatus.ToLower(),
                cancellationToken);
        var partFavorites = await _context.ProductFavorites
            .CountAsync(
                favorite =>
                    favorite.UserId == userId
                    && _context.ShopProducts.Any(product => product.Id == favorite.ProductId && product.IsActive),
                cancellationToken);
        var favorites = carFavorites + partFavorites;
        var storeInboxId = asStoreStaff
            ? await ResolveStoreInboxUserIdAsync(cancellationToken)
            : null;
        var unreadMessages = await _context.Messages
            .CountAsync(
                message =>
                    !message.IsRead
                    && (message.ReceiverId == userId
                        || (storeInboxId != null
                            && message.ReceiverId == storeInboxId
                            && _context.Conversations.Any(conversation =>
                                conversation.Id == message.ConversationId
                                && conversation.SellerId == storeInboxId))),
                cancellationToken);

        var listingActivities = await _context.Cars
            .AsNoTracking()
            .Where(car => car.OwnerId == userId)
            .OrderByDescending(car => car.CreatedAt)
            .Take(5)
            .Select(car => new
            {
                Type = "listing",
                Description = $"Listed {car.Brand.Name} {car.Model}",
                car.CreatedAt,
                Url = $"/Cars/Details/{car.Id}"
            })
            .ToListAsync(cancellationToken);
        var carFavoriteActivities = await _context.Favorites
            .AsNoTracking()
            .Where(favorite =>
                favorite.UserId == userId
                && favorite.Car.Status.ToLower() == CarPublicListing.ActiveStatus.ToLower())
            .OrderByDescending(favorite => favorite.CreatedAt)
            .Take(5)
            .Select(favorite => new
            {
                Type = "favorite",
                Description = $"Favorited {favorite.Car.Brand.Name} {favorite.Car.Model}",
                favorite.CreatedAt,
                Url = $"/Cars/Details/{favorite.CarId}"
            })
            .ToListAsync(cancellationToken);
        var partFavoriteActivities = await (
            from favorite in _context.ProductFavorites.AsNoTracking()
            join product in _context.ShopProducts.AsNoTracking()
                on favorite.ProductId equals product.Id
            where favorite.UserId == userId && product.IsActive
            orderby favorite.CreatedAt descending
            select new
            {
                Type = "favorite",
                Description = $"Favorited {product.Name}",
                favorite.CreatedAt,
                Url = $"/Shop/Details/{product.Id}"
            })
            .Take(5)
            .ToListAsync(cancellationToken);
        var favoriteActivities = carFavoriteActivities
            .Concat(partFavoriteActivities)
            .OrderByDescending(item => item.CreatedAt)
            .Take(5)
            .ToList();
        var messageActivities = await _context.Messages
            .AsNoTracking()
            .Where(message => message.SenderId == userId || message.ReceiverId == userId)
            .OrderByDescending(message => message.CreatedAt)
            .Take(5)
            .Select(message => new
            {
                Type = "message",
                Description = message.SenderId == userId ? "Sent a message" : "Received a message",
                message.CreatedAt,
                Url = $"/Account/Messages/{message.ConversationId}"
            })
            .ToListAsync(cancellationToken);

        var activity = listingActivities
            .Concat(favoriteActivities)
            .Concat(messageActivities)
            .OrderByDescending(item => item.CreatedAt)
            .Take(5)
            .Select(item => new AccountActivityResponse(
                ActivityKey(item.Type, item.Url, item.CreatedAt),
                item.Type,
                item.Description,
                item.CreatedAt,
                item.Url))
            .ToList();

        return new AccountSummaryResponse(
            activeListings,
            favorites,
            unreadMessages,
            activity);
    }

    private async Task ClearConversationHidesAsync(
        int conversationId,
        CancellationToken cancellationToken)
    {
        var hides = await _context.ConversationHides
            .Where(hide => hide.ConversationId == conversationId)
            .ToListAsync(cancellationToken);
        if (hides.Count == 0)
        {
            return;
        }

        _context.ConversationHides.RemoveRange(hides);
    }

    private static ProductFavoriteResponse MapProductFavorite(
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

    private async Task<bool> UserExistsAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        return await _context.IdentityUsers
            .AnyAsync(user => user.Id == userId, cancellationToken);
    }

    /// <summary>
    /// Private listings use the car owner. Store catalog cars (no OwnerId) route to the
    /// configured dealer inbox account so buyers can still negotiate via Messages.
    /// </summary>
    private async Task<string?> ResolveSellerIdAsync(
        Car car,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(car.OwnerId)
            && await UserExistsAsync(car.OwnerId, cancellationToken))
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

        var email = inboxEmail.Trim();
        return await _context.IdentityUsers
            .AsNoTracking()
            .Where(user => user.Email != null && user.Email == email)
            .Select(user => user.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<bool> CanAccessConversationAsync(
        Conversation conversation,
        string userId,
        bool asStoreStaff,
        CancellationToken cancellationToken)
    {
        if (conversation.BuyerId == userId || conversation.SellerId == userId)
        {
            return true;
        }

        if (!asStoreStaff)
        {
            return false;
        }

        var storeInboxId = await ResolveStoreInboxUserIdAsync(cancellationToken);
        return storeInboxId != null && conversation.SellerId == storeInboxId;
    }

    private static ConversationDetailsResponse ToDetails(Conversation conversation)
    {
        return new ConversationDetailsResponse(
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
                .Select(message => new MessageResponse(
                    message.Id,
                    message.SenderId,
                    DisplayName(message.Sender),
                    message.ReceiverId,
                    MarketplaceMessagingRules.StripGeneratedListingText(message.Content),
                    message.IsRead,
                    message.CreatedAt))
                .ToList());
    }

    private static string ActivityKey(string type, string? url, DateTime createdAt) =>
        $"{type}|{url ?? string.Empty}|{createdAt.Ticks}";

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
