using PCS_API.Models;

namespace PCS_API.Repositories;

public interface IPartnerBankAccountRepository
{
    Task<IEnumerable<PartnerBankAccountModel>> GetByPartnerAsync(string partnerName, int? supplierId, CancellationToken cancellationToken = default);
    Task<PartnerBankAccountModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(PartnerBankAccountModel model, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(PartnerBankAccountModel model, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string partnerName, string accountNo, CancellationToken cancellationToken = default);
}
