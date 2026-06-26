using PCS_API.DTOs;

namespace PCS_API.Services;

public interface ISalesOrderService
{
    Task<PagedResultDto<SalesOrderListDto>> GetPagedAsync(SalesOrderSearchDto search, CancellationToken cancellationToken = default);
    Task<SalesOrderDetailDto?> GetByIdAsync(int orderId, CancellationToken cancellationToken = default);
    Task<ResultDto<int>> CreateAsync(SalesOrderCreateDto dto, CancellationToken cancellationToken = default);
    Task<ResultDto<bool>> CompleteOrderAsync(int orderId, int? updatedBy, CancellationToken cancellationToken = default);
    Task<ResultDto<bool>> CancelOrderAsync(int orderId, int? updatedBy, CancellationToken cancellationToken = default);
}
