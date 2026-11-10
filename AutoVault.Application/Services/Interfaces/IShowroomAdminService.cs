using FinalProject.DTOs;

namespace FinalProject.Services.Interfaces;

public interface IShowroomAdminService
{
    Task<IReadOnlyList<ShowroomRequestItemDto>> ListRequestsAsync(
        string? status = null,
        CancellationToken cancellationToken = default);

    Task<ShowroomRequestItemDto?> GetRequestAsync(int id, CancellationToken cancellationToken = default);

    Task<(ShowroomRequestItemDto? Item, string? Error)> SetRequestStatusAsync(
        int id,
        string status,
        CancellationToken cancellationToken = default);

    Task<(bool Found, string? Error)> DeleteRequestAsync(int id, CancellationToken cancellationToken = default);

    Task<(ShowroomSaleItemDto? Sale, string? Error)> RecordSaleAsync(
        RecordShowroomSaleRequestDto request,
        string recordedByUserId,
        CancellationToken cancellationToken = default);

    Task<(ShowroomSaleItemDto? Sale, string? Error)> CompleteRequestWithSaleAsync(
        int requestId,
        string recordedByUserId,
        string? note,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ShowroomSaleItemDto>> ListSalesAsync(CancellationToken cancellationToken = default);

    Task<(bool Ok, string? Error)> VoidSaleAsync(
        int saleId,
        string voidedByUserId,
        CancellationToken cancellationToken = default);
}
