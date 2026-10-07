using FinalProject.MVC.Models;

namespace FinalProject.MVC.Services.Interfaces;

public interface IOrderService
{
    Task<Order> PlaceOrderAsync(
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
        CancellationToken cancellationToken = default);

    Task<Order?> GetOrderAsync(int id, CancellationToken cancellationToken = default);
}
