using FinalProject.MVC.Commerce;
using FinalProject.MVC.Models;
using Microsoft.EntityFrameworkCore;

namespace FinalProject.MVC.Repositories.Implementations;

public sealed partial class ApplicationRepository
{
    public void AddContactRequest(ContactRequest request) => _db.ContactRequests.Add(request);

    public async Task<IReadOnlyList<ContactRequest>> GetContactRequestsAsync(
        string? status,
        CancellationToken cancellationToken = default)
    {
        var query = _db.ContactRequests.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(status) && ContactRequestStatus.IsValid(status))
        {
            query = query.Where(request => request.Status == status);
        }

        return await query
            .OrderByDescending(request => request.CreatedAt)
            .ThenByDescending(request => request.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ContactRequest>> GetContactRequestsForUserAsync(
        string userId,
        CancellationToken cancellationToken = default) =>
        await _db.ContactRequests.AsNoTracking()
            .Where(request => request.UserId == userId)
            .OrderByDescending(request => request.CreatedAt)
            .ThenByDescending(request => request.Id)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ContactRequest>> FindOrphanedContactRequestsByEmailAsync(
        string targetEmail,
        CancellationToken cancellationToken = default) =>
        await _db.ContactRequests
            .Where(request =>
                request.UserId == null
                && request.Email != null
                && request.Email.ToLower() == targetEmail)
            .ToListAsync(cancellationToken);

    public Task<ContactRequest?> FindTrackedContactRequestAsync(int id, CancellationToken cancellationToken = default) =>
        _db.ContactRequests.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

    public void RemoveContactRequest(ContactRequest request) => _db.ContactRequests.Remove(request);
}
