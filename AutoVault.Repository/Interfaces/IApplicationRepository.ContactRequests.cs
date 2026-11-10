using FinalProject.MVC.Models;

namespace FinalProject.MVC.Repositories;

public partial interface IApplicationRepository
{
    void AddContactRequest(ContactRequest request);
    Task<IReadOnlyList<ContactRequest>> GetContactRequestsAsync(string? status, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ContactRequest>> GetContactRequestsForUserAsync(string userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ContactRequest>> FindOrphanedContactRequestsByEmailAsync(string targetEmail, CancellationToken cancellationToken = default);
    Task<ContactRequest?> FindTrackedContactRequestAsync(int id, CancellationToken cancellationToken = default);
    void RemoveContactRequest(ContactRequest request);
}
