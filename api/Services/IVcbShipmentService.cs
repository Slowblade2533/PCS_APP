using PCS_API.DTOs;

namespace PCS_API.Services;

public interface IVcbShipmentService
{
    Task<ResultDto<PagedResultDto<VcbShipmentDto>>> GetPagedAsync(VcbShipmentSearchDto search, CancellationToken cancellationToken = default);
    Task<ResultDto<VcbShipmentDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ResultDto<int>> CreateAsync(VcbShipmentCreateDto dto, int currentUserId, CancellationToken cancellationToken = default);
    Task<ResultDto<bool>> UpdateStatusAsync(int id, string status, int currentUserId, CancellationToken cancellationToken = default);
    Task<ResultDto<bool>> UpdateAsync(int id, VcbShipmentCreateDto dto, int currentUserId, bool isSuperuser, CancellationToken cancellationToken = default);
}
