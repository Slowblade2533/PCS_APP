using PCS_API.DTOs;

namespace PCS_API.Repositories;

public interface IChartOfAccountRepository
{
    Task<IEnumerable<ChartOfAccountDto>> GetAllAccountsAsync(CancellationToken cancellationToken = default);
    Task<ChartOfAccountDto?> GetAccountByIdAsync(int accountId, CancellationToken cancellationToken = default);
    Task<int> CheckDuplicateAccountCodeAsync(string accountCode, CancellationToken cancellationToken = default);
    Task<int> InsertAccountAsync(ChartOfAccountCreateDto dto, CancellationToken cancellationToken = default);
}
