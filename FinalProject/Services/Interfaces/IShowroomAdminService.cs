using FinalProject.DTOs;

namespace FinalProject.Services.Interfaces;

public interface IShowroomAdminService
{
    Task<IReadOnlyList<ShowroomRequestItem>> ListRequestsAsync(
        string? status = null,
        CancellationToken cancellationToken = default);

    Task<ShowroomRequestItem?> GetRequestAsync(int id, CancellationToken cancellationToken = default);

    Task<(ShowroomRequestItem? Item, string? Error)> SetRequestStatusAsync(
        int id,
        string status,
        CancellationToken cancellationToken = default);

    Task<(bool Found, string? Error)> DeleteRequestAsync(int id, CancellationToken cancellationToken = default);

    Task<(ShowroomSaleItem? Sale, string? Error)> RecordSaleAsync(
        RecordShowroomSaleRequest request,
        string recordedByUserId,
        CancellationToken cancellationToken = default);

    Task<(ShowroomSaleItem? Sale, string? Error)> CompleteRequestWithSaleAsync(
        int requestId,
        string recordedByUserId,
        string? note,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ShowroomSaleItem>> ListSalesAsync(CancellationToken cancellationToken = default);

    Task<(bool Ok, string? Error)> VoidSaleAsync(
        int saleId,
        string voidedByUserId,
        CancellationToken cancellationToken = default);
}
