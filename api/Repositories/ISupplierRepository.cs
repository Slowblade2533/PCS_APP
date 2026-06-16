using PCS_API.DTOs;
using PCS_API.Models;

namespace PCS_API.Repositories;

public interface ISupplierRepository
{
    Task<SupplierModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<PagedResultDto<SupplierDto>> GetPagedAsync(SupplierSearchDto search, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(SupplierModel model, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(SupplierModel model, CancellationToken cancellationToken = default);
    Task<bool> ExistsByCodeAsync(string code, int? excludeId = null, CancellationToken cancellationToken = default);
}
