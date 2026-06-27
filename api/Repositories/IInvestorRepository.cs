using PCS_API.Models;

namespace PCS_API.Repositories;

public interface IInvestorRepository
{
    Task<IEnumerable<InvestorModel>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<InvestorModel?> GetByIdAsync(Guid investorId, CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(InvestorModel model, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(InvestorModel model, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid investorId, CancellationToken cancellationToken = default);
}
