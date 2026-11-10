using FinalProject.Commerce;
using FinalProject.Repositories;
using FinalProject.DTOs;
using FinalProject.Models.Entities;
using FinalProject.Services.Interfaces;

namespace FinalProject.Services;

public sealed class ShowroomAdminService : IShowroomAdminService
{
    private readonly IShowroomRepository _db;

    public ShowroomAdminService(IShowroomRepository db)
    {
        _db = db;
    }

    public async Task<(ShowroomSaleItemDto? Sale, string? Error)> RecordSaleAsync(
        RecordShowroomSaleRequestDto request,
        string recordedByUserId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(recordedByUserId))
        {
            return (null, "Recorded-by user is required.");
        }

        var durableKey = request.IdempotencyKey is { } key && key != Guid.Empty
            ? key.ToString("N")
            : null;

        var product = await _db.GetProductByIdAsync(request.ProductId, cancellationToken);
        if (product == null)
        {
            return (null, "Product not found.");
        }

        if (!product.IsActive)
        {
            return (null, "Inactive products cannot be sold in store.");
        }

        var qtyError = ShowroomApiRules.ValidateSaleAgainstReservations(
            request.Quantity,
            product.Quantity,
            openReservedQuantity: 0);
        if (qtyError != null)
        {
            return (null, qtyError);
        }

        var result = await _db.RecordInPersonSaleAsync(
            request.ProductId,
            request.Quantity,
            recordedByUserId,
            (request.Note ?? string.Empty).Trim(),
            durableKey,
            cancellationToken);
        return result.Success
            ? (MapSale(result.Sale!), null)
            : (null, result.Error);
    }

    public async Task<IReadOnlyList<ShowroomSaleItemDto>> ListSalesAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _db.ListRecentSalesAsync(cancellationToken);
        return rows.Select(MapSale).ToList();
    }

    public async Task<(bool Ok, string? Error)> VoidSaleAsync(
        int saleId,
        string voidedByUserId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(voidedByUserId))
        {
            return (false, "Voided-by user is required.");
        }

        var result = await _db.VoidSaleAsync(saleId, voidedByUserId, cancellationToken);
        return (result.Success, result.Error);
    }

    private static ShowroomSaleItemDto MapSale(InPersonSaleReference sale)
    {
        return new ShowroomSaleItemDto
        {
            Id = sale.Id,
            ProductId = sale.ProductId,
            ProductName = sale.Product?.Name,
            Quantity = sale.Quantity,
            UnitPrice = sale.UnitPrice,
            ListUnitPrice = sale.ListUnitPrice,
            CampaignId = sale.CampaignId,
            CampaignDiscountPercent = sale.CampaignDiscountPercent,
            Note = sale.Note,
            SoldAt = sale.SoldAt,
            RecordedByUserId = sale.RecordedByUserId
        };
    }
}
