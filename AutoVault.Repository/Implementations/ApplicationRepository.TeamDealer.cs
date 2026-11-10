using FinalProject.MVC.Models;
using Microsoft.EntityFrameworkCore;

namespace FinalProject.MVC.Repositories.Implementations;

public sealed partial class ApplicationRepository
{
    public async Task<IReadOnlyList<TeamMember>> GetPublishedTeamMembersAsync(CancellationToken cancellationToken = default) =>
        await _db.TeamMembers.AsNoTracking()
            .Where(member => member.IsPublished)
            .OrderBy(member => member.SortOrder)
            .ThenBy(member => member.Id)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<TeamMember>> GetAllTeamMembersAsync(CancellationToken cancellationToken = default) =>
        await _db.TeamMembers.AsNoTracking()
            .OrderBy(member => member.SortOrder)
            .ThenBy(member => member.Id)
            .ToListAsync(cancellationToken);

    public Task<TeamMember?> GetTeamMemberByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _db.TeamMembers.AsNoTracking().FirstOrDefaultAsync(member => member.Id == id, cancellationToken);

    public Task<bool> AnyTeamMembersAsync(CancellationToken cancellationToken = default) =>
        _db.TeamMembers.AnyAsync(cancellationToken);

    public async Task SeedTeamMembersWithIdentityInsertAsync(
        IReadOnlyList<TeamMember> members,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        await _db.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT TeamMembers ON", cancellationToken);
        _db.TeamMembers.AddRange(members);
        await SaveChangesAsync(cancellationToken);
        await _db.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT TeamMembers OFF", cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public Task<int?> GetMaxTeamMemberSortOrderAsync(CancellationToken cancellationToken = default) =>
        _db.TeamMembers.Select(m => (int?)m.SortOrder).MaxAsync(cancellationToken);

    public void AddTeamMember(TeamMember member) => _db.TeamMembers.Add(member);

    public Task<TeamMember?> FindTrackedTeamMemberAsync(int id, CancellationToken cancellationToken = default) =>
        _db.TeamMembers.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

    public void RemoveTeamMember(TeamMember member) => _db.TeamMembers.Remove(member);

    public async Task<IReadOnlyList<TeamMember>> GetTeamMembersOrderedAsync(CancellationToken cancellationToken = default) =>
        await _db.TeamMembers.OrderBy(m => m.SortOrder).ThenBy(m => m.Id).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Dealer>> GetPublishedDealersAsync(CancellationToken cancellationToken = default) =>
        await _db.Dealers.AsNoTracking()
            .Where(dealer => dealer.IsPublished)
            .OrderBy(dealer => dealer.SortOrder)
            .ThenBy(dealer => dealer.Id)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Dealer>> GetAllDealersAsync(CancellationToken cancellationToken = default) =>
        await _db.Dealers.AsNoTracking()
            .OrderBy(dealer => dealer.SortOrder)
            .ThenBy(dealer => dealer.Id)
            .ToListAsync(cancellationToken);

    public Task<Dealer?> GetDealerByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _db.Dealers.AsNoTracking().FirstOrDefaultAsync(dealer => dealer.Id == id, cancellationToken);

    public Task<bool> AnyDealersAsync(CancellationToken cancellationToken = default) =>
        _db.Dealers.AnyAsync(cancellationToken);

    public async Task SeedDealersWithIdentityInsertAsync(
        IReadOnlyList<Dealer> dealers,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        await _db.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT Dealers ON", cancellationToken);
        _db.Dealers.AddRange(dealers);
        await SaveChangesAsync(cancellationToken);
        await _db.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT Dealers OFF", cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public Task<int?> GetMaxDealerSortOrderAsync(CancellationToken cancellationToken = default) =>
        _db.Dealers.Select(d => (int?)d.SortOrder).MaxAsync(cancellationToken);

    public void AddDealer(Dealer dealer) => _db.Dealers.Add(dealer);

    public Task<Dealer?> FindTrackedDealerAsync(int id, CancellationToken cancellationToken = default) =>
        _db.Dealers.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public void RemoveDealer(Dealer dealer) => _db.Dealers.Remove(dealer);

    public async Task<IReadOnlyList<Dealer>> GetDealersOrderedAsync(CancellationToken cancellationToken = default) =>
        await _db.Dealers.OrderBy(d => d.SortOrder).ThenBy(d => d.Id).ToListAsync(cancellationToken);
}
