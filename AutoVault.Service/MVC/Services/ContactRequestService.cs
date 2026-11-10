using FinalProject.MVC.Commerce;
using FinalProject.MVC.Models;
using FinalProject.MVC.Repositories;
using FinalProject.MVC.Services.Interfaces;

namespace FinalProject.MVC.Services;

public sealed class ContactRequestService : IContactRequestService
{
    private readonly IContactRequestRepository _db;

    public ContactRequestService(IContactRequestRepository db)
    {
        _db = db;
    }

    public async Task<(bool Ok, int? Id, Dictionary<string, string> Errors, string? Error)> SaveAsync(
        ContactFormModel model,
        string? userId = null,
        CancellationToken cancellationToken = default)
    {
        var errors = ContactFormRules.Validate(model);
        if (errors.Count > 0)
        {
            return (false, null, errors, "Please correct the highlighted fields.");
        }

        var normalizedUserId = string.IsNullOrWhiteSpace(userId) ? null : userId.Trim();

        var entity = new ContactRequest
        {
            CustomerName = model.Name,
            Email = model.Email,
            Phone = model.Phone,
            Interest = model.Car,
            Message = model.Message,
            Status = ContactRequestStatus.New,
            UserId = normalizedUserId,
            CreatedAt = DateTime.UtcNow
        };

        _db.AddContactRequest(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return (true, entity.Id, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase), null);
    }

    public Task<IReadOnlyList<ContactRequest>> GetAllAsync(
        string? status = null,
        CancellationToken cancellationToken = default) =>
        _db.GetContactRequestsAsync(status, cancellationToken);

    public Task<IReadOnlyList<ContactRequest>> GetForUserAsync(
        string userId,
        CancellationToken cancellationToken = default) =>
        _db.GetContactRequestsForUserAsync(userId, cancellationToken);

    public async Task<int> ClaimOrphanedByVerifiedEmailAsync(
        string userId,
        string accountEmail,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(accountEmail))
        {
            return 0;
        }

        var targetEmail = accountEmail.Trim().ToLowerInvariant();
        var orphans = await _db.FindOrphanedContactRequestsByEmailAsync(targetEmail, cancellationToken);

        if (orphans.Count == 0)
        {
            return 0;
        }

        foreach (var orphan in orphans)
        {
            orphan.UserId = userId.Trim();
            orphan.UpdatedAt ??= DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return orphans.Count;
    }

    public async Task<(bool Ok, string? Error)> SetStatusAsync(int id, string status, CancellationToken cancellationToken = default)
    {
        if (!ContactRequestStatus.IsValid(status))
        {
            return (false, "Status is not allowed.");
        }

        var request = await _db.FindTrackedContactRequestAsync(id, cancellationToken);
        if (request is null)
        {
            return (false, "Contact request not found.");
        }

        if (!ContactRequestStatus.CanTransition(request.Status, status))
        {
            return (false, $"Cannot change status from {request.Status} to {status}.");
        }

        if (string.Equals(request.Status, status, StringComparison.Ordinal))
        {
            return (true, null);
        }

        request.Status = status;
        request.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var request = await _db.FindTrackedContactRequestAsync(id, cancellationToken);
        if (request is null)
        {
            return false;
        }

        _db.RemoveContactRequest(request);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
