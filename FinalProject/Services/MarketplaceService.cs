using FinalProject.Data;
using FinalProject.DTOs;
using FinalProject.Mappings;
using FinalProject.Models.Entities;
using FinalProject.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinalProject.Services;

public sealed class MarketplaceService : IMarketplaceService
{
    private readonly FinalProjectDbContext _context;

    public MarketplaceService(FinalProjectDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<FavoriteResponse>> GetFavoritesAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var favorites = await _context.Favorites
            .AsNoTracking()
            .Where(favorite => favorite.UserId == userId)
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
            .Where(favorite => favorite.UserId == userId)
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
            || !await _context.Cars.AnyAsync(car => car.Id == carId, cancellationToken))
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
        CancellationToken cancellationToken = default)
    {
        var conversations = await _context.Conversations
            .AsNoTracking()
            .Where(conversation =>
                conversation.BuyerId == userId || conversation.SellerId == userId)
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
            var other = conversation.BuyerId == userId
                ? conversation.Seller
                : conversation.Buyer;
            var lastMessage = conversation.Messages
                .OrderByDescending(message => message.CreatedAt)
                .FirstOrDefault();

            return new ConversationSummaryResponse(
                conversation.Id,
                conversation.CarId,
                CarName(conversation.Car),
                MainImage(conversation.Car),
                DisplayName(other),
                lastMessage?.Content ?? string.Empty,
                conversation.UpdatedAt,
                conversation.Messages.Count(message =>
                    message.ReceiverId == userId && !message.IsRead));
        }).ToList();
    }

    public async Task<ConversationDetailsResponse?> GetConversationAsync(
        int id,
        string userId,
        bool markRead,
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
            || (conversation.BuyerId != userId && conversation.SellerId != userId))
        {
            return null;
        }

        if (markRead)
        {
            var unread = conversation.Messages
                .Where(message => message.ReceiverId == userId && !message.IsRead)
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
            .SingleOrDefaultAsync(item => item.Id == carId, cancellationToken);
        if (car?.OwnerId == null
            || car.OwnerId == buyerId
            || !await UserExistsAsync(buyerId, cancellationToken))
        {
            return null;
        }

        var conversation = await _context.Conversations
            .SingleOrDefaultAsync(item =>
                item.CarId == carId
                && item.BuyerId == buyerId
                && item.SellerId == car.OwnerId,
                cancellationToken);
        var now = DateTime.UtcNow;
        if (conversation == null)
        {
            conversation = new Conversation
            {
                CarId = carId,
                BuyerId = buyerId,
                SellerId = car.OwnerId,
                Subject = string.IsNullOrWhiteSpace(request.Subject)
                    ? null
                    : request.Subject.Trim(),
                CreatedAt = now,
                UpdatedAt = now
            };
            _context.Conversations.Add(conversation);
        }
        else
        {
            conversation.UpdatedAt = now;
        }

        conversation.Messages.Add(new CarMessage
        {
            SenderId = buyerId,
            ReceiverId = car.OwnerId,
            Content = request.Content.Trim(),
            CreatedAt = now
        });

        await _context.SaveChangesAsync(cancellationToken);
        return await GetConversationAsync(
            conversation.Id,
            buyerId,
            markRead: false,
            cancellationToken);
    }

    public async Task<MessageResponse?> ReplyAsync(
        int conversationId,
        string senderId,
        string content,
        CancellationToken cancellationToken = default)
    {
        var conversation = await _context.Conversations
            .SingleOrDefaultAsync(item => item.Id == conversationId, cancellationToken);
        if (conversation == null
            || (conversation.BuyerId != senderId && conversation.SellerId != senderId))
        {
            return null;
        }

        var receiverId = conversation.BuyerId == senderId
            ? conversation.SellerId
            : conversation.BuyerId;
        var now = DateTime.UtcNow;
        var message = new CarMessage
        {
            ConversationId = conversationId,
            SenderId = senderId,
            ReceiverId = receiverId,
            Content = content.Trim(),
            CreatedAt = now
        };
        conversation.UpdatedAt = now;
        _context.Messages.Add(message);
        await _context.SaveChangesAsync(cancellationToken);

        var sender = await _context.IdentityUsers
            .AsNoTracking()
            .SingleAsync(user => user.Id == senderId, cancellationToken);
        return new MessageResponse(
            message.Id,
            message.SenderId,
            DisplayName(sender),
            message.ReceiverId,
            message.Content,
            message.IsRead,
            message.CreatedAt);
    }

    public async Task<AccountSummaryResponse> GetSummaryAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var activeListings = await _context.Cars
            .CountAsync(
                car => car.OwnerId == userId && car.Status == "Active",
                cancellationToken);
        var favorites = await _context.Favorites
            .CountAsync(favorite => favorite.UserId == userId, cancellationToken);
        var unreadMessages = await _context.Messages
            .CountAsync(
                message => message.ReceiverId == userId && !message.IsRead,
                cancellationToken);

        var listingActivities = await _context.Cars
            .AsNoTracking()
            .Where(car => car.OwnerId == userId)
            .OrderByDescending(car => car.CreatedAt)
            .Take(5)
            .Select(car => new AccountActivityResponse(
                "listing",
                $"Listed {car.Brand.Name} {car.Model}",
                car.CreatedAt,
                $"/Cars/Details/{car.Id}"))
            .ToListAsync(cancellationToken);
        var favoriteActivities = await _context.Favorites
            .AsNoTracking()
            .Where(favorite => favorite.UserId == userId)
            .OrderByDescending(favorite => favorite.CreatedAt)
            .Take(5)
            .Select(favorite => new AccountActivityResponse(
                "favorite",
                $"Favorited {favorite.Car.Brand.Name} {favorite.Car.Model}",
                favorite.CreatedAt,
                $"/Cars/Details/{favorite.CarId}"))
            .ToListAsync(cancellationToken);
        var messageActivities = await _context.Messages
            .AsNoTracking()
            .Where(message => message.SenderId == userId || message.ReceiverId == userId)
            .OrderByDescending(message => message.CreatedAt)
            .Take(5)
            .Select(message => new AccountActivityResponse(
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

        return new AccountSummaryResponse(
            activeListings,
            favorites,
            unreadMessages,
            activity);
    }

    private async Task<bool> UserExistsAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        return await _context.IdentityUsers
            .AnyAsync(user => user.Id == userId, cancellationToken);
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
                    message.Content,
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
