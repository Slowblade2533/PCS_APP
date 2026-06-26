using PCS_API.DTOs;
using PCS_API.Models;

namespace PCS_API.Repositories;

public interface IVcbShipmentRepository
{
    Task<VcbShipmentDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<PagedResultDto<VcbShipmentDto>> GetPagedAsync(VcbShipmentSearchDto search, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(VcbShipmentCreateDto dto, int createdBy, System.Data.IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<bool> UpdateStatusAsync(int id, string status, int currentUserId, System.Data.IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(int id, VcbShipmentCreateDto dto, int currentUserId, bool isSuperuser, System.Data.IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    
    // Extracted read methods for Orchestration
    Task<(int DeliveryId, string Status, bool IsForceCloseOrder)> GetShipmentInfoAsync(int id, System.Data.IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<VcbShipmentItemModel>> GetShipmentItemsAsync(int id, System.Data.IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> GetReceivedShipmentBoxesAsync(int deliveryId, int? excludeShipmentId = null, System.Data.IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<List<int>> GetOrderIdsByShipmentIdAsync(int shipmentId, System.Data.IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<List<int>> GetFulfilledOrderIdsAsync(int shipmentId, System.Data.IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<int> GetBranchIdByOrderIdAsync(int orderId, System.Data.IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
}
