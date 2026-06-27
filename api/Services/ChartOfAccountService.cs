using PCS_API.DTOs;
using PCS_API.Repositories;

namespace PCS_API.Services;

public class ChartOfAccountService(IChartOfAccountRepository accountRepo) : IChartOfAccountService
{
    public async Task<IEnumerable<ChartOfAccountDto>> GetAllAccountsAsync(CancellationToken cancellationToken = default)
    {
        return await accountRepo.GetAllAccountsAsync(cancellationToken);
    }

    public async Task<ChartOfAccountDto?> GetAccountByIdAsync(int accountId, CancellationToken cancellationToken = default)
    {
        return await accountRepo.GetAccountByIdAsync(accountId, cancellationToken);
    }

    public async Task<ResultDto<int>> CreateAccountAsync(ChartOfAccountCreateDto dto, CancellationToken cancellationToken = default)
    {
        int existing = await accountRepo.CheckDuplicateAccountCodeAsync(dto.AccountCode, cancellationToken);
        if (existing > 0)
            return ResultDto<int>.Failure($"รหัสบัญชี '{dto.AccountCode}' มีอยู่ในระบบแล้ว");

        int newId = await accountRepo.InsertAccountAsync(dto, cancellationToken);
        return ResultDto<int>.Success(newId);
    }
}
