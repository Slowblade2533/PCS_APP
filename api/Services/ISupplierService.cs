using PCS_API.DTOs;

namespace PCS_API.Services;

public interface ISupplierService
{
    Task<ResultDto<PagedResultDto<SupplierDto>>> GetPagedAsync(SupplierSearchDto search, CancellationToken cancellationToken = default);
    Task<ResultDto<SupplierDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ResultDto<int>> CreateAsync(SupplierCreateDto dto, CancellationToken cancellationToken = default);
    Task<ResultDto<bool>> UpdateAsync(int id, SupplierCreateDto dto, CancellationToken cancellationToken = default);
}
