using FinalProject.MVC.Models;

namespace FinalProject.MVC.Services.Interfaces;

public interface ITeamMemberService
{
    Task<IReadOnlyList<TeamMember>> GetPublishedAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TeamMember>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<TeamMember?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task EnsureSeedAsync(CancellationToken cancellationToken = default);

    Task<(bool Ok, string? Error, int? Id)> CreateAsync(
        TeamMemberEditViewModel model,
        IFormFile? image,
        CancellationToken cancellationToken = default);

    Task<(bool Ok, string? Error)> UpdateAsync(
        TeamMemberEditViewModel model,
        IFormFile? image,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);

    Task<bool> SetPublishedAsync(int id, bool isPublished, CancellationToken cancellationToken = default);

    Task<(bool Ok, string? Error)> MoveAsync(int id, int direction, CancellationToken cancellationToken = default);
}
