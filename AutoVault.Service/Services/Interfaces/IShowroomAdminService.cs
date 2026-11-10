using FinalProject.DTOs;

namespace FinalProject.Services.Interfaces;

public interface IShowroomAdminService
{
    Task<(ShowroomSaleItemDto? Sale, string? Error)> RecordSaleAsync(
        RecordShowroomSaleRequestDto request,
        string recordedByUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ShowroomSaleItemDto>> ListSalesAsync(CancellationToken cancellationToken = default);

    Task<(bool Ok, string? Error)> VoidSaleAsync(
        int saleId,
        string voidedByUserId,
        CancellationToken cancellationToken = default);
}
