using FinalProject.MVC.Repositories;
using Microsoft.EntityFrameworkCore.Storage;

namespace FinalProject.MVC.Repositories.Implementations;

internal sealed class ApplicationRepositoryTransaction : IApplicationRepositoryTransaction
{
    private readonly IDbContextTransaction _transaction;

    public ApplicationRepositoryTransaction(IDbContextTransaction transaction)
    {
        _transaction = transaction;
    }

    public Task CommitAsync(CancellationToken cancellationToken = default) =>
        _transaction.CommitAsync(cancellationToken);

    public Task RollbackAsync(CancellationToken cancellationToken = default) =>
        _transaction.RollbackAsync(cancellationToken);

    public ValueTask DisposeAsync() => _transaction.DisposeAsync();
}
