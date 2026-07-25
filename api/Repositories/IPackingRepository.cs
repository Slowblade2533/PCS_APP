using PCS_API.DTOs;
using System.Data;

namespace PCS_API.Repositories;

public interface IPackingRepository
{
    Task<int> CreateBatchAsync(CreatePackingBatchDto dto, int? userId, IDbTransaction? transaction = null);
    Task CreateBatchItemAsync(int batchId, PackingBatchItemCreateDto item, IDbTransaction? transaction = null);
    Task<PagedResultDto<PackingBatchResponseDto>> GetBatchesPagedAsync(PaginationParamsDto @params, CancellationToken cancellationToken = default);
    Task<PackingBatchResponseDto?> GetBatchByIdAsync(int batchId, CancellationToken cancellationToken = default);
    Task<int?> FindVariantIdBySkuAsync(string sku, IDbTransaction? transaction = null);
    Task<Dictionary<string, (int VariantId, string VariantName)>> CheckSkusExistAsync(IEnumerable<string> skus, CancellationToken cancellationToken = default);
    Task DeductStockForShipmentAsync(int variantId, int quantity, string referenceDoc, string notes, int? userId, IDbTransaction? transaction = null);
    Task UpdateItemShippedQuantityAsync(long batchItemId, int shippedQty, string packStatus, IDbTransaction? transaction = null);
    Task CreatePendingResolutionAsync(ResolveBackorderDto dto, int? userId, IDbTransaction? transaction = null);
    Task<List<PackingBatchItemResponseDto>> GetPendingBackordersAsync(CancellationToken cancellationToken = default);
}
