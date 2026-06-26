using PCS_API.DTOs;

namespace PCS_API.Repositories;

public interface IVcbOrderRepository
{
    Task<VcbOrderDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<PagedResultDto<VcbOrderDto>> GetPagedAsync(VcbOrderSearchDto search, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(VcbOrderCreateDto dto, string? transferSlipUrl, int createdBy, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(int id, VcbOrderCreateDto dto, string? transferSlipUrl, int updatedBy, CancellationToken cancellationToken = default);
    Task<bool> UpdateStatusAsync(int id, string status, int currentUserId, System.Data.IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
}
