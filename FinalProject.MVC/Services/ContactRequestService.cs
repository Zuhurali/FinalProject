using FinalProject.MVC.Commerce;
using FinalProject.MVC.Data;
using FinalProject.MVC.Models;
using FinalProject.MVC.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinalProject.MVC.Services;

public sealed class ContactRequestService : IContactRequestService
{
    private readonly ApplicationDbContext _db;

    public ContactRequestService(ApplicationDbContext db)
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

        _db.ContactRequests.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return (true, entity.Id, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase), null);
    }

    public async Task<IReadOnlyList<ContactRequest>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _db.ContactRequests.AsNoTracking()
            .OrderByDescending(request => request.CreatedAt)
            .ThenByDescending(request => request.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ContactRequest>> GetForUserAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Array.Empty<ContactRequest>();
        }

        return await _db.ContactRequests.AsNoTracking()
            .Where(request => request.UserId == userId)
            .OrderByDescending(request => request.CreatedAt)
            .ThenByDescending(request => request.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> ClaimOrphanedByVerifiedEmailAsync(
        string userId,
        string accountEmail,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(accountEmail))
        {
            return 0;
        }

        var targetEmail = accountEmail.Trim();
        var candidates = await _db.ContactRequests
            .Where(request => request.UserId == null)
            .ToListAsync(cancellationToken);

        var orphans = candidates
            .Where(request =>
                !string.IsNullOrWhiteSpace(request.Email)
                && string.Equals(request.Email.Trim(), targetEmail, StringComparison.OrdinalIgnoreCase))
            .ToList();

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

    public async Task<bool> SetStatusAsync(int id, string status, CancellationToken cancellationToken = default)
    {
        if (!ContactRequestStatus.IsValid(status))
        {
            return false;
        }

        var request = await _db.ContactRequests.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (request is null)
        {
            return false;
        }

        request.Status = status;
        request.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var request = await _db.ContactRequests.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (request is null)
        {
            return false;
        }

        _db.ContactRequests.Remove(request);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
