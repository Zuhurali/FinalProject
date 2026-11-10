using FinalProject.MVC.Models;

namespace FinalProject.MVC.Repositories;

public partial interface IApplicationRepository
{
    Task<IReadOnlyList<TeamMember>> GetPublishedTeamMembersAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TeamMember>> GetAllTeamMembersAsync(CancellationToken cancellationToken = default);
    Task<TeamMember?> GetTeamMemberByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> AnyTeamMembersAsync(CancellationToken cancellationToken = default);
    Task SeedTeamMembersWithIdentityInsertAsync(IReadOnlyList<TeamMember> members, CancellationToken cancellationToken = default);
    Task<int?> GetMaxTeamMemberSortOrderAsync(CancellationToken cancellationToken = default);
    void AddTeamMember(TeamMember member);
    Task<TeamMember?> FindTrackedTeamMemberAsync(int id, CancellationToken cancellationToken = default);
    void RemoveTeamMember(TeamMember member);
    Task<IReadOnlyList<TeamMember>> GetTeamMembersOrderedAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Dealer>> GetPublishedDealersAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Dealer>> GetAllDealersAsync(CancellationToken cancellationToken = default);
    Task<Dealer?> GetDealerByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> AnyDealersAsync(CancellationToken cancellationToken = default);
    Task SeedDealersWithIdentityInsertAsync(IReadOnlyList<Dealer> dealers, CancellationToken cancellationToken = default);
    Task<int?> GetMaxDealerSortOrderAsync(CancellationToken cancellationToken = default);
    void AddDealer(Dealer dealer);
    Task<Dealer?> FindTrackedDealerAsync(int id, CancellationToken cancellationToken = default);
    void RemoveDealer(Dealer dealer);
    Task<IReadOnlyList<Dealer>> GetDealersOrderedAsync(CancellationToken cancellationToken = default);
}
