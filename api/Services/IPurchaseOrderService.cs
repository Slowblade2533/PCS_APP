using PCS_API.DTOs;

namespace PCS_API.Services;

public interface IPurchaseOrderService
{
    Task<PagedResultDto<PurchaseOrderListDto>> GetPagedAsync(PurchaseOrderSearchDto search, CancellationToken cancellationToken = default);
    Task<PurchaseOrderDetailDto?> GetByIdAsync(int purchaseOrderId, CancellationToken cancellationToken = default);
    Task<ResultDto<int>> CreateAsync(PurchaseOrderCreateDto dto, CancellationToken cancellationToken = default);
    Task<ResultDto<bool>> UpdateStatusAsync(int purchaseOrderId, PurchaseOrderStatusUpdateDto dto, CancellationToken cancellationToken = default);
    Task<ResultDto<bool>> UpdateSlipAsync(int purchaseOrderId, string slipUrl, int? updatedBy, CancellationToken cancellationToken = default);
    Task<ResultDto<bool>> UpdateAsync(int purchaseOrderId, PurchaseOrderCreateDto dto, CancellationToken cancellationToken = default);
    Task<ResultDto<bool>> DeleteAsync(int purchaseOrderId, CancellationToken cancellationToken = default);
}
