using FinalProject.MVC.Models;

namespace FinalProject.MVC.Services.Interfaces;

public interface IShopCatalogService
{
    Task<IReadOnlyList<ShopProduct>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<ShopCatalogProduct?> GetRecordAsync(int id, CancellationToken cancellationToken = default);

    Task<ShopCatalogProduct> CreateAsync(ShopCatalogProduct product, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(ShopCatalogProduct product, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);

    Task<int> CountAsync(CancellationToken cancellationToken = default);

    Task EnsureSeedAsync(CancellationToken cancellationToken = default);
}
