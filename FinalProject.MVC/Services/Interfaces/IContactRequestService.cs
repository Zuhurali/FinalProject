using FinalProject.MVC.Models;

namespace FinalProject.MVC.Services.Interfaces;

public interface IContactRequestService
{
    Task<(bool Ok, int? Id, Dictionary<string, string> Errors, string? Error)> SaveAsync(
        ContactFormModel model,
        string? userId = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ContactRequest>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ContactRequest>> GetForUserAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Links orphaned requests (null UserId) to a user only when the form email
    /// matches that user's account email (case-insensitive). Never attaches
    /// requests with a different or unmatched email.
    /// </summary>
    Task<int> ClaimOrphanedByVerifiedEmailAsync(
        string userId,
        string accountEmail,
        CancellationToken cancellationToken = default);

    Task<bool> SetStatusAsync(int id, string status, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
