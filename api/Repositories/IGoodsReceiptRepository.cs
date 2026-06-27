using PCS_API.DTOs;
using System.Data;

namespace PCS_API.Repositories;

public interface IGoodsReceiptRepository
{
    Task<PagedResultDto<GoodsReceiptListDto>> GetPagedAsync(GoodsReceiptSearchDto search, CancellationToken cancellationToken = default);
    Task<GoodsReceiptDetailDto?> GetByIdAsync(int receiptId, CancellationToken cancellationToken = default);
    
    Task<(int PurchaseOrderId, string Status)?> CheckPurchaseOrderStatusAsync(int poId, IDbTransaction? tx = null);
    Task<int> InsertReceiptAndItemsAsync(GoodsReceiptCreateDto dto, IDbTransaction tx);
    
    Task<GoodsReceiptDetailDto?> GetBasicInfoAsync(int receiptId, IDbTransaction? tx = null);
    Task<List<GoodsReceiptItemDto>> GetItemsAsync(int receiptId, IDbTransaction? tx = null);
    Task UpdatePurchaseOrderItemsReceivedQtyAsync(long poItemId, int qty, IDbTransaction tx);
    Task UpdateReceiptStatusAsync(int receiptId, string status, int? updatedBy, IDbTransaction tx);
    
    Task<List<(int Quantity, int ReceivedQuantity)>> GetPurchaseOrderItemsQtyAsync(int poId, IDbTransaction tx);
    Task UpdatePurchaseOrderStatusAsync(int poId, string status, IDbTransaction tx);
}
