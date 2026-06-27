using PCS_API.DTOs;

namespace PCS_API.Repositories;

public interface IVcbDeliveryRepository
{
    Task<PagedResultDto<VcbDeliveryDto>> GetPagedAsync(VcbDeliverySearchDto search, CancellationToken cancellationToken = default);
    Task<VcbDeliveryDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(VcbDeliveryCreateDto dto, string? transferSlipUrl, int currentUserId, System.Data.IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(int id, VcbDeliveryCreateDto dto, string? transferSlipUrl, int currentUserId, System.Data.IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<bool> UpdateStatusAsync(int id, string status, int currentUserId, System.Data.IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> GetDeliveryBoxesAsync(int deliveryId, System.Data.IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
}
