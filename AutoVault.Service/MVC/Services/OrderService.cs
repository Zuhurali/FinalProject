using FinalProject.MVC.Models;
using FinalProject.MVC.Repositories;
using FinalProject.MVC.Services.Interfaces;

namespace FinalProject.MVC.Services;

public sealed class OrderService : IOrderService
{
    private readonly IOrderRepository _db;
    private readonly ICouponService _coupons;

    public OrderService(IOrderRepository db, ICouponService coupons)
    {
        _db = db;
        _coupons = coupons;
    }

  /// <summary>
    /// Legacy writer for historical <c>Orders</c> rows. Public MVC checkout must not call this —
    /// AutoVault is contact / in-store only. Prefer in-person <c>RecordSaleAsync</c> for new sales.
    /// </summary>
    public async Task<Order> PlaceOrderAsync(
        string? userId,
        string firstName,
        string lastName,
        string email,
        string phoneNumber,
        string streetAddress,
        string city,
        string district,
        string? postCode,
        string? companyName,
        string? orderNotes,
        CartViewModel cart,
        CancellationToken cancellationToken = default)
    {
        decimal discount = 0m;
        string? couponCode = null;
        if (!string.IsNullOrWhiteSpace(cart.CouponCode) && !string.IsNullOrWhiteSpace(userId))
        {
            var (ok, error, amount, code) = await _coupons.PreviewApplyAsync(
                cart.CouponCode,
                userId,
                cart,
                cancellationToken);
            if (!ok || code == null)
            {
                throw new InvalidOperationException(error ?? "The applied coupon is no longer valid.");
            }

            discount = amount;
            couponCode = code;
        }

        await using var transaction = await _db.BeginTransactionAsync(cancellationToken);
        var order = new Order
        {
            UserId = userId,
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            PhoneNumber = phoneNumber,
            StreetAddress = streetAddress,
            City = city,
            District = district,
            PostCode = postCode,
            CompanyName = companyName,
            OrderNotes = orderNotes,
            Subtotal = cart.Subtotal,
            DiscountAmount = discount,
            CouponCode = couponCode,
            Total = cart.Subtotal - discount,
            OrderDate = DateTime.UtcNow,
            OrderStatus = "Pending"
        };

        foreach (var cartItem in cart.Items)
        {
            order.OrderItems.Add(new OrderItem
            {
                ProductId = cartItem.ProductId,
                ProductName = cartItem.ProductName,
                Price = cartItem.Price,
                Quantity = cartItem.Quantity,
                Subtotal = cartItem.Subtotal,
                ImagePath = cartItem.ImagePath
            });
        }

        _db.AddOrder(order);
        await _db.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(userId) && !string.IsNullOrWhiteSpace(couponCode))
        {
            var (redeemed, redeemError) = await _coupons.RedeemAsync(
                userId,
                couponCode,
                discount,
                cancellationToken);
            if (!redeemed)
            {
                await transaction.RollbackAsync(cancellationToken);
                throw new InvalidOperationException(redeemError ?? "Coupon could not be redeemed.");
            }
        }

        await transaction.CommitAsync(cancellationToken);
        return order;
    }

    public Task<Order?> GetOrderAsync(int id, CancellationToken cancellationToken = default) =>
        _db.GetOrderWithItemsByIdAsync(id, cancellationToken);

    public Task<IReadOnlyList<Order>> GetRecentOrdersAsync(
        int take = 100,
        CancellationToken cancellationToken = default) =>
        _db.GetRecentOrdersWithItemsAsync(take, cancellationToken);
}
