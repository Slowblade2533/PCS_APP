using Microsoft.AspNetCore.Http;
using PCS_API.DTOs;

namespace PCS_API.Services;

public interface IVcbOrderService
{
    Task<ResultDto<PagedResultDto<VcbOrderDto>>> GetPagedAsync(VcbOrderSearchDto search, CancellationToken cancellationToken = default);
    Task<ResultDto<VcbOrderDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ResultDto<int>> CreateAsync(VcbOrderCreateDto dto, IFormFile? slipFile, int currentUserId, CancellationToken cancellationToken = default);
    Task<ResultDto<bool>> UpdateAsync(int id, VcbOrderCreateDto dto, IFormFile? slipFile, int currentUserId, CancellationToken cancellationToken = default);
    Task<ResultDto<bool>> UpdateStatusAsync(int id, string status, CancellationToken cancellationToken = default);
}
