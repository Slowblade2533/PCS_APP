using PCS_API.DTOs;

namespace PCS_API.Services;

public interface IChartOfAccountService
{
    Task<IEnumerable<ChartOfAccountDto>> GetAllAccountsAsync(CancellationToken cancellationToken = default);
    Task<ChartOfAccountDto?> GetAccountByIdAsync(int accountId, CancellationToken cancellationToken = default);
    Task<ResultDto<int>> CreateAccountAsync(ChartOfAccountCreateDto dto, CancellationToken cancellationToken = default);
}
