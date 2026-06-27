using PCS_API.Models;

namespace PCS_API.Services;

public interface IInvestorService
{
    Task<IEnumerable<InvestorModel>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<InvestorModel?> GetByIdAsync(Guid investorId, CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(InvestorModel model, List<InvestorBankAccountModel> bankAccounts, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(InvestorModel model, List<InvestorBankAccountModel> bankAccounts, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid investorId, CancellationToken cancellationToken = default);
    Task<IEnumerable<InvestorBankAccountModel>> GetBankAccountsAsync(Guid investorId, CancellationToken cancellationToken = default);
}
