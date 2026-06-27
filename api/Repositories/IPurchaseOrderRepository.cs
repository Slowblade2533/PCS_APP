using PCS_API.DTOs;
using System.Data;

namespace PCS_API.Repositories;

public interface IPurchaseOrderRepository
{
    Task<PagedResultDto<PurchaseOrderListDto>> GetPagedAsync(PurchaseOrderSearchDto search, CancellationToken cancellationToken = default);
    Task<PurchaseOrderDetailDto?> GetByIdAsync(int purchaseOrderId, CancellationToken cancellationToken = default);
    
    Task<int> InsertOrderAndItemsAsync(PurchaseOrderCreateDto dto, decimal subTotal, decimal vatAmount, decimal grandTotal, IDbTransaction tx);
    Task<int> UpdateOrderAndItemsAsync(int purchaseOrderId, PurchaseOrderCreateDto dto, decimal subTotal, decimal vatAmount, decimal grandTotal, IDbTransaction tx);
    Task<int> UpdateStatusAsync(int purchaseOrderId, string status, int? updatedBy, IDbTransaction? tx = null);
    Task<int> UpdateSlipAsync(int purchaseOrderId, string slipUrl, int? updatedBy, IDbTransaction? tx = null);
    Task<int> DeleteAsync(int purchaseOrderId, IDbTransaction tx);
}
