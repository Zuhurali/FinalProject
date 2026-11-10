using FinalProject.MVC.Models;
using Microsoft.EntityFrameworkCore;

namespace FinalProject.MVC.Repositories.Implementations;

public sealed partial class ApplicationRepository
{
    public void AddOrder(Order order) => _db.Orders.Add(order);

    public Task<Order?> GetOrderWithItemsByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _db.Orders
            .AsNoTracking()
            .Include(order => order.OrderItems)
            .FirstOrDefaultAsync(order => order.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Order>> GetRecentOrdersWithItemsAsync(
        int take,
        CancellationToken cancellationToken = default)
    {
        var limit = take < 1 ? 100 : Math.Min(take, 500);
        return await _db.Orders
            .AsNoTracking()
            .Include(order => order.OrderItems)
            .OrderByDescending(order => order.OrderDate)
            .ThenByDescending(order => order.Id)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }
}
