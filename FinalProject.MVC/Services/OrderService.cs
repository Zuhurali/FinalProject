using FinalProject.MVC.Data;
using FinalProject.MVC.Models;
using FinalProject.MVC.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinalProject.MVC.Services;

public sealed class OrderService : IOrderService
{
    private readonly ApplicationDbContext _db;
    private readonly ICouponService _coupons;

    public OrderService(ApplicationDbContext db, ICouponService coupons)
    {
        _db = db;
        _coupons = coupons;
    }

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
        // Recalculate coupon discount on the server — never trust session amounts alone.
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

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
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

        _db.Orders.Add(order);
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

    public async Task<Order?> GetOrderAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _db.Orders
            .AsNoTracking()
            .Include(order => order.OrderItems)
            .FirstOrDefaultAsync(order => order.Id == id, cancellationToken);
    }
}
