using System.Data;
using PCS_API.Models;

namespace PCS_API.Repositories;

public interface IAccountTransactionRepository
{
    Task<int> CreateAsync(AccountTransactionModel model, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<int?> GetIdByReferenceAsync(string referenceType, int referenceId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task UpdateAsync(int id, decimal amount, string notes, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
}
