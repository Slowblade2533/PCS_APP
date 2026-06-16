using PCS_API.DTOs;

namespace PCS_API.Repositories;

public interface IVcbShipmentRepository
{
    Task<VcbShipmentDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<PagedResultDto<VcbShipmentDto>> GetPagedAsync(VcbShipmentSearchDto search, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(VcbShipmentCreateDto dto, int createdBy, CancellationToken cancellationToken = default);
    Task<bool> UpdateStatusAsync(int id, string status, int currentUserId, CancellationToken cancellationToken = default);
}
