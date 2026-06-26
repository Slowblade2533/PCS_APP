using PCS_API.DTOs;

namespace PCS_API.Services;

public interface IVcbDeliveryService
{
    Task<PagedResultDto<VcbDeliveryDto>> GetPagedAsync(VcbDeliverySearchDto search, CancellationToken cancellationToken = default);
    Task<VcbDeliveryDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(VcbDeliveryCreateDto dto, IFormFile? slipFile, int currentUserId, CancellationToken cancellationToken = default);
    Task<ResultDto<bool>> UpdateAsync(int id, VcbDeliveryCreateDto dto, IFormFile? slipFile, int currentUserId, CancellationToken cancellationToken = default);
    Task<ResultDto<bool>> UpdateStatusAsync(int id, string status, int currentUserId, CancellationToken cancellationToken = default);
}
