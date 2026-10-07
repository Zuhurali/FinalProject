using FinalProject.MVC.Data;
using FinalProject.MVC.Models;
using FinalProject.MVC.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinalProject.MVC.Services;

public sealed class OrderService : IOrderService
{
    private readonly ApplicationDbContext _db;

    public OrderService(ApplicationDbContext db)
    {
        _db = db;
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
            DiscountAmount = cart.DiscountAmount,
            CouponCode = cart.CouponCode,
            Total = cart.Total,
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
