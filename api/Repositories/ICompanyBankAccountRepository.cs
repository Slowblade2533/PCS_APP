using PCS_API.Models;

namespace PCS_API.Repositories;

public interface ICompanyBankAccountRepository
{
    Task<IEnumerable<CompanyBankAccountModel>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    Task<CompanyBankAccountModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(CompanyBankAccountModel model, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(CompanyBankAccountModel model, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
