using PCS_API.Models;

namespace PCS_API.Repositories;

public interface IAccountTransactionRepository
{
    Task<int> CreateAsync(AccountTransactionModel model, CancellationToken cancellationToken = default);
}
