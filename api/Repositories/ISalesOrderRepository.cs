using PCS_API.DTOs;
using System.Data;

namespace PCS_API.Repositories;

public interface ISalesOrderRepository
{
    Task<PagedResultDto<SalesOrderListDto>> GetPagedAsync(SalesOrderSearchDto search, CancellationToken cancellationToken = default);
    Task<SalesOrderDetailDto?> GetByIdAsync(int orderId, CancellationToken cancellationToken = default);
    
    Task<Dictionary<int, decimal>> GetVariantBasePricesAsync(List<int> variantIds, IDbTransaction tx);
    Task<int> InsertOrderAndItemsAsync(SalesOrderCreateDto dto, string orderNo, decimal subTotal, decimal discountTotal, decimal vatAmount, decimal grandTotal, Dictionary<int, decimal> priceMap, IDbTransaction tx);
    
    Task<SalesOrderDetailDto?> GetBasicInfoAsync(int orderId, IDbTransaction? tx = null);
    Task<List<SalesOrderItemDto>> GetItemsAsync(int orderId, IDbTransaction? tx = null);
    Task UpdateStatusAsync(int orderId, string status, int? updatedBy, IDbTransaction? tx = null);
}
