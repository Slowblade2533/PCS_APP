using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PCS_API.Models;

namespace PCS_API.Repositories
{
    public interface IInvestorBankAccountRepository
    {
        Task<IEnumerable<InvestorBankAccountModel>> GetByInvestorAsync(Guid investorId, CancellationToken cancellationToken = default);
        Task<InvestorBankAccountModel?> GetByIdAsync(Guid bankAccountId, CancellationToken cancellationToken = default);
        Task<Guid> CreateAsync(InvestorBankAccountModel model, CancellationToken cancellationToken = default);
        Task<bool> UpdateAsync(InvestorBankAccountModel model, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(Guid bankAccountId, CancellationToken cancellationToken = default);
    }
}
