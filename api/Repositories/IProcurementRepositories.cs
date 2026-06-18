using PCS_API.DTOs;
using System.Data;

namespace PCS_API.Repositories;

public interface ISalesOrderRepository
{
    Task<PagedResultDto<SalesOrderListDto>> GetPagedAsync(SalesOrderSearchDto search, CancellationToken cancellationToken = default);
    Task<SalesOrderDetailDto?> GetByIdAsync(int orderId);
    Task<ResultDto<int>> CreateAsync(SalesOrderCreateDto dto);
    Task<ResultDto<bool>> CompleteOrderAsync(int orderId, int? updatedBy);
    Task<ResultDto<bool>> CancelOrderAsync(int orderId, int? updatedBy);
}

public interface IPurchaseOrderRepository
{
    Task<PagedResultDto<PurchaseOrderListDto>> GetPagedAsync(PurchaseOrderSearchDto search, CancellationToken cancellationToken = default);
    Task<PurchaseOrderDetailDto?> GetByIdAsync(int purchaseOrderId);
    Task<ResultDto<int>> CreateAsync(PurchaseOrderCreateDto dto);
    Task<ResultDto<bool>> UpdateStatusAsync(int purchaseOrderId, PurchaseOrderStatusUpdateDto dto);
    Task<ResultDto<bool>> UpdateSlipAsync(int purchaseOrderId, string slipUrl, int? updatedBy);
}

public interface IGoodsReceiptRepository
{
    Task<PagedResultDto<GoodsReceiptListDto>> GetPagedAsync(GoodsReceiptSearchDto search, CancellationToken cancellationToken = default);
    Task<GoodsReceiptDetailDto?> GetByIdAsync(int receiptId);
    Task<ResultDto<int>> CreateAsync(GoodsReceiptCreateDto dto);
    Task<ResultDto<bool>> CompleteReceiptAsync(int receiptId, int? updatedBy);
}
