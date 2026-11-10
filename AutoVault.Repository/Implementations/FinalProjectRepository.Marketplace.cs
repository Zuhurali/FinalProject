using FinalProject.Models.Entities;
using FinalProject.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FinalProject.Repositories.Implementations;

public sealed partial class FinalProjectRepository
{
    private const string ActiveCarStatus = "Active";

    public Task<string?> ResolveUserIdByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var trimmed = email.Trim();
        return _db.IdentityUsers.AsNoTracking()
            .Where(user => user.Email != null && user.Email == trimmed)
            .Select(user => user.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Favorite>> GetCarFavoritesForUserAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var activeLower = ActiveCarStatus.ToLower();
        return await _db.Favorites
            .AsNoTracking()
            .Where(favorite =>
                favorite.UserId == userId
                && favorite.Car.Status.ToLower() == activeLower)
            .OrderByDescending(favorite => favorite.CreatedAt)
            .Include(favorite => favorite.Car.Brand)
            .Include(favorite => favorite.Car.Category)
            .Include(favorite => favorite.Car.Owner)
            .Include(favorite => favorite.Car.Images)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<int>> GetCarFavoriteIdsAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var activeLower = ActiveCarStatus.ToLower();
        return await _db.Favorites
            .AsNoTracking()
            .Where(favorite =>
                favorite.UserId == userId
                && favorite.Car.Status.ToLower() == activeLower)
            .OrderBy(favorite => favorite.CarId)
            .Select(favorite => favorite.CarId)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> ActiveCarExistsAsync(int carId, CancellationToken cancellationToken = default)
    {
        var activeLower = ActiveCarStatus.ToLower();
        return _db.Cars.AnyAsync(
            car => car.Id == carId && car.Status.ToLower() == activeLower,
            cancellationToken);
    }

    public Task<Favorite?> FindCarFavoriteTrackedAsync(
        string userId,
        int carId,
        CancellationToken cancellationToken = default) =>
        _db.Favorites.SingleOrDefaultAsync(
            favorite => favorite.UserId == userId && favorite.CarId == carId,
            cancellationToken);

    public async Task<Favorite> AddOrGetCarFavoriteAsync(
        string userId,
        int carId,
        CancellationToken cancellationToken = default)
    {
        var existing = await FindCarFavoriteTrackedAsync(userId, carId, cancellationToken);
        if (existing != null)
        {
            return existing;
        }

        existing = new Favorite
        {
            UserId = userId,
            CarId = carId,
            CreatedAt = DateTime.UtcNow
        };
        _db.Favorites.Add(existing);
        await _db.SaveChangesAsync(cancellationToken);
        return existing;
    }

    public Task<Favorite?> GetCarFavoriteWithCarAsync(int favoriteId, CancellationToken cancellationToken = default) =>
        _db.Favorites
            .AsNoTracking()
            .Include(item => item.Car.Brand)
            .Include(item => item.Car.Category)
            .Include(item => item.Car.Owner)
            .Include(item => item.Car.Images)
            .AsSplitQuery()
            .SingleOrDefaultAsync(item => item.Id == favoriteId, cancellationToken);

    public async Task<bool> RemoveCarFavoriteAsync(
        string userId,
        int carId,
        CancellationToken cancellationToken = default)
    {
        var favorite = await FindCarFavoriteTrackedAsync(userId, carId, cancellationToken);
        if (favorite == null)
        {
            return false;
        }

        _db.Favorites.Remove(favorite);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<ProductFavoriteProductRow>> GetProductFavoritesForUserAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await _db.ProductFavorites
            .AsNoTracking()
            .Where(favorite => favorite.UserId == userId)
            .OrderByDescending(favorite => favorite.CreatedAt)
            .Join(
                _db.ShopProducts.AsNoTracking().Where(product => product.IsActive),
                favorite => favorite.ProductId,
                product => product.Id,
                (favorite, product) => new ProductFavoriteProductRow(
                    favorite.Id,
                    favorite.CreatedAt,
                    product.Id,
                    product.Name,
                    product.Price,
                    product.Category,
                    product.Brand,
                    product.ImagePath,
                    product.Quantity,
                    product.InStock))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<int>> GetProductFavoriteIdsAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await _db.ProductFavorites
            .AsNoTracking()
            .Where(favorite => favorite.UserId == userId)
            .Join(
                _db.ShopProducts.AsNoTracking().Where(product => product.IsActive),
                favorite => favorite.ProductId,
                product => product.Id,
                (favorite, product) => product.Id)
            .OrderBy(id => id)
            .ToListAsync(cancellationToken);
    }

    public Task<ShopProductReference?> GetActiveProductAsync(
        int productId,
        CancellationToken cancellationToken = default) =>
        _db.ShopProducts.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == productId && item.IsActive, cancellationToken);

    public Task<ProductFavorite?> FindProductFavoriteTrackedAsync(
        string userId,
        int productId,
        CancellationToken cancellationToken = default) =>
        _db.ProductFavorites.SingleOrDefaultAsync(
            favorite => favorite.UserId == userId && favorite.ProductId == productId,
            cancellationToken);

    public async Task<ProductFavorite> AddOrGetProductFavoriteAsync(
        string userId,
        int productId,
        CancellationToken cancellationToken = default)
    {
        var existing = await FindProductFavoriteTrackedAsync(userId, productId, cancellationToken);
        if (existing != null)
        {
            return existing;
        }

        existing = new ProductFavorite
        {
            UserId = userId,
            ProductId = productId,
            CreatedAt = DateTime.UtcNow
        };
        _db.ProductFavorites.Add(existing);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            _db.ChangeTracker.Clear();
            existing = await FindProductFavoriteTrackedAsync(userId, productId, cancellationToken);
            if (existing == null)
            {
                throw;
            }
        }

        return existing;
    }

    public async Task<bool> RemoveProductFavoriteAsync(
        string userId,
        int productId,
        CancellationToken cancellationToken = default)
    {
        var favorite = await FindProductFavoriteTrackedAsync(userId, productId, cancellationToken);
        if (favorite == null)
        {
            return false;
        }

        _db.ProductFavorites.Remove(favorite);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<int>> GetHiddenConversationIdsAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await _db.ConversationHides
            .AsNoTracking()
            .Where(hide => hide.UserId == userId)
            .Select(hide => hide.ConversationId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Conversation>> GetInboxConversationsAsync(
        string userId,
        IReadOnlyCollection<int> hiddenConversationIds,
        CancellationToken cancellationToken = default)
    {
        return await _db.Conversations
            .AsNoTracking()
            .Where(conversation =>
                (conversation.BuyerId == userId || conversation.SellerId == userId)
                && !hiddenConversationIds.Contains(conversation.Id))
            .OrderByDescending(conversation => conversation.UpdatedAt)
            .Include(conversation => conversation.Car.Brand)
            .Include(conversation => conversation.Car.Images)
            .Include(conversation => conversation.Buyer)
            .Include(conversation => conversation.Seller)
            .Include(conversation => conversation.Messages)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);
    }

    public Task<Conversation?> GetConversationForDetailsAsync(
        int conversationId,
        CancellationToken cancellationToken = default) =>
        _db.Conversations
            .Include(item => item.Car.Brand)
            .Include(item => item.Car.Images)
            .Include(item => item.Buyer)
            .Include(item => item.Seller)
            .Include(item => item.Messages)
                .ThenInclude(message => message.Sender)
            .AsSplitQuery()
            .SingleOrDefaultAsync(item => item.Id == conversationId, cancellationToken);

    public async Task<int> MarkConversationMessagesReadAsync(
        int conversationId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var conversation = await GetConversationForDetailsAsync(conversationId, cancellationToken);
        if (conversation == null)
        {
            return 0;
        }

        var unread = conversation.Messages
            .Where(message => message.ReceiverId == userId && !message.IsRead)
            .ToList();
        if (unread.Count == 0)
        {
            return 0;
        }

        unread.ForEach(message => message.IsRead = true);
        await _db.SaveChangesAsync(cancellationToken);
        return unread.Count;
    }

    public Task<Car?> GetActiveCarWithBrandAsync(int carId, CancellationToken cancellationToken = default)
    {
        var activeLower = ActiveCarStatus.ToLower();
        return _db.Cars
            .AsNoTracking()
            .Include(item => item.Brand)
            .SingleOrDefaultAsync(
                item => item.Id == carId && item.Status.ToLower() == activeLower,
                cancellationToken);
    }

    public Task<Conversation?> FindConversationByParticipantsAsync(
        int carId,
        string buyerId,
        string sellerId,
        CancellationToken cancellationToken = default) =>
        _db.Conversations.SingleOrDefaultAsync(item =>
            item.CarId == carId
            && item.BuyerId == buyerId
            && item.SellerId == sellerId,
            cancellationToken);

    public async Task<MarketplaceConversationStartResult> PersistConversationStartAsync(
        int carId,
        string buyerId,
        string sellerId,
        string subject,
        string content,
        CancellationToken cancellationToken = default)
    {
        var conversation = await FindConversationByParticipantsAsync(carId, buyerId, sellerId, cancellationToken);
        var now = DateTime.UtcNow;

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
            _db.Conversations.Add(conversation);
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

        await ClearConversationHidesInternalAsync(conversation.Id, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        return new MarketplaceConversationStartResult(true, conversation, message);
    }

    public Task<Conversation?> GetConversationTrackedAsync(
        int conversationId,
        CancellationToken cancellationToken = default) =>
        _db.Conversations.SingleOrDefaultAsync(item => item.Id == conversationId, cancellationToken);

    public async Task<MarketplaceReplyPersistResult> PersistReplyAsync(
        int conversationId,
        string senderId,
        string receiverId,
        string content,
        CancellationToken cancellationToken = default)
    {
        var conversation = await GetConversationTrackedAsync(conversationId, cancellationToken);
        if (conversation == null)
        {
            return new MarketplaceReplyPersistResult(false, null, null);
        }

        var now = DateTime.UtcNow;
        var message = new CarMessage
        {
            ConversationId = conversationId,
            SenderId = senderId,
            ReceiverId = receiverId,
            Content = content,
            CreatedAt = now
        };
        conversation.UpdatedAt = now;
        _db.Messages.Add(message);
        await ClearConversationHidesInternalAsync(conversationId, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        var sender = await GetIdentityUserAsync(senderId, cancellationToken);
        return new MarketplaceReplyPersistResult(true, message, sender);
    }

    public Task<Conversation?> GetConversationForAccessCheckAsync(
        int conversationId,
        CancellationToken cancellationToken = default) =>
        _db.Conversations.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == conversationId, cancellationToken);

    public async Task<bool> HideConversationForUserAsync(
        int conversationId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var existing = await _db.ConversationHides.SingleOrDefaultAsync(
            hide => hide.ConversationId == conversationId && hide.UserId == userId,
            cancellationToken);
        if (existing != null)
        {
            return true;
        }

        _db.ConversationHides.Add(new ConversationHide
        {
            ConversationId = conversationId,
            UserId = userId,
            HiddenAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<MarketplaceAccountSummaryRow> GetAccountSummaryAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var activeLower = ActiveCarStatus.ToLower();
        var activeListings = await _db.Cars
            .CountAsync(car => car.OwnerId == userId && car.Status == ActiveCarStatus, cancellationToken);
        var carFavorites = await _db.Favorites
            .CountAsync(
                favorite =>
                    favorite.UserId == userId
                    && favorite.Car.Status.ToLower() == activeLower,
                cancellationToken);
        var partFavorites = await _db.ProductFavorites
            .CountAsync(
                favorite =>
                    favorite.UserId == userId
                    && _db.ShopProducts.Any(product => product.Id == favorite.ProductId && product.IsActive),
                cancellationToken);
        var favorites = carFavorites + partFavorites;
        var unreadMessages = await _db.Messages
            .CountAsync(message => !message.IsRead && message.ReceiverId == userId, cancellationToken);

        var listingActivities = await _db.Cars
            .AsNoTracking()
            .Where(car => car.OwnerId == userId)
            .OrderByDescending(car => car.CreatedAt)
            .Take(5)
            .Select(car => new MarketplaceAccountActivityRow(
                ActivityKey("listing", $"/Cars/Details/{car.Id}", car.CreatedAt),
                "listing",
                $"Listed {car.Brand.Name} {car.Model}",
                car.CreatedAt,
                $"/Cars/Details/{car.Id}"))
            .ToListAsync(cancellationToken);

        var carFavoriteActivities = await _db.Favorites
            .AsNoTracking()
            .Where(favorite =>
                favorite.UserId == userId
                && favorite.Car.Status.ToLower() == activeLower)
            .OrderByDescending(favorite => favorite.CreatedAt)
            .Take(5)
            .Select(favorite => new MarketplaceAccountActivityRow(
                ActivityKey("favorite", $"/Cars/Details/{favorite.CarId}", favorite.CreatedAt),
                "favorite",
                $"Favorited {favorite.Car.Brand.Name} {favorite.Car.Model}",
                favorite.CreatedAt,
                $"/Cars/Details/{favorite.CarId}"))
            .ToListAsync(cancellationToken);

        var partFavoriteActivities = await (
            from favorite in _db.ProductFavorites.AsNoTracking()
            join product in _db.ShopProducts.AsNoTracking()
                on favorite.ProductId equals product.Id
            where favorite.UserId == userId && product.IsActive
            orderby favorite.CreatedAt descending
            select new MarketplaceAccountActivityRow(
                ActivityKey("favorite", $"/Shop/Details/{product.Id}", favorite.CreatedAt),
                "favorite",
                $"Favorited {product.Name}",
                favorite.CreatedAt,
                $"/Shop/Details/{product.Id}"))
            .Take(5)
            .ToListAsync(cancellationToken);

        var favoriteActivities = carFavoriteActivities
            .Concat(partFavoriteActivities)
            .OrderByDescending(item => item.CreatedAt)
            .Take(5)
            .ToList();

        var messageActivities = await _db.Messages
            .AsNoTracking()
            .Where(message => message.SenderId == userId || message.ReceiverId == userId)
            .OrderByDescending(message => message.CreatedAt)
            .Take(5)
            .Select(message => new MarketplaceAccountActivityRow(
                ActivityKey("message", $"/Account/Messages/{message.ConversationId}", message.CreatedAt),
                "message",
                message.SenderId == userId ? "Sent a message" : "Received a message",
                message.CreatedAt,
                $"/Account/Messages/{message.ConversationId}"))
            .ToListAsync(cancellationToken);

        var activity = listingActivities
            .Concat(favoriteActivities)
            .Concat(messageActivities)
            .OrderByDescending(item => item.CreatedAt)
            .Take(5)
            .ToList();

        return new MarketplaceAccountSummaryRow(activeListings, favorites, unreadMessages, activity);
    }

    public Task<IdentityUserReference> GetIdentityUserAsync(
        string userId,
        CancellationToken cancellationToken = default) =>
        _db.IdentityUsers.AsNoTracking().SingleAsync(user => user.Id == userId, cancellationToken);

    private async Task ClearConversationHidesInternalAsync(int conversationId, CancellationToken cancellationToken)
    {
        var hides = await _db.ConversationHides
            .Where(hide => hide.ConversationId == conversationId)
            .ToListAsync(cancellationToken);
        if (hides.Count == 0)
        {
            return;
        }

        _db.ConversationHides.RemoveRange(hides);
    }

    private static string ActivityKey(string type, string? url, DateTime createdAt) =>
        $"{type}|{url ?? string.Empty}|{createdAt.Ticks}";
}
