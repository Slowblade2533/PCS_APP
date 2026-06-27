using PCS_API.DTOs;
using PCS_API.Repositories;
using System.Text.Json;
using System.Data;
using PCS_API.Models;

namespace PCS_API.Services;

public class VcbShipmentService(
        IVcbShipmentRepository shipmentRepository,
        IVcbDeliveryRepository deliveryRepository,
        IVcbOrderRepository orderRepository,
        IStockRepository stockRepository,
        IAccountTransactionRepository accountTransactionRepo,
        ISqlConnectionFactory connectionFactory,
        ILogger<VcbShipmentService> logger) : IVcbShipmentService
{
    public async Task<ResultDto<PagedResultDto<VcbShipmentDto>>> GetPagedAsync(VcbShipmentSearchDto search, CancellationToken cancellationToken = default)
    {
        var result = await shipmentRepository.GetPagedAsync(search, cancellationToken);
        return ResultDto<PagedResultDto<VcbShipmentDto>>.Success(result);
    }

    public async Task<ResultDto<VcbShipmentDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var shipment = await shipmentRepository.GetByIdAsync(id, cancellationToken);
        if (shipment == null)
            return ResultDto<VcbShipmentDto>.Failure("ไม่พบข้อมูลใบรับสินค้า VCANBUY");

        return ResultDto<VcbShipmentDto>.Success(shipment);
    }

    public async Task<ResultDto<int>> CreateAsync(VcbShipmentCreateDto dto, int currentUserId, CancellationToken cancellationToken = default)
    {
        if (dto.Items == null || dto.Items.Count == 0)
            return ResultDto<int>.Failure("กรุณาระบุรายการสินค้าอย่างน้อย 1 รายการ");

        var groupedItems = dto.Items.GroupBy(i => i.OrderItemId);
        foreach (var group in groupedItems)
        {
            var firstItem = group.First();
            int expectedQuantity = firstItem.ExpectedQuantity;
            int totalReceived = group.Sum(i => i.GoodQuantity + i.DefectiveQuantity);
            string status = firstItem.ReceiptStatus;

            if (status == "Complete" && totalReceived != expectedQuantity)
                return ResultDto<int>.Failure("สถานะรับครบ จำนวนรวมต้องเท่ากับจำนวนที่สั่ง");
            
            if (status == "Incomplete" && totalReceived >= expectedQuantity)
                return ResultDto<int>.Failure("สถานะรับไม่ครบ จำนวนรวมต้องน้อยกว่าจำนวนที่สั่ง");

            if (status == "Over" && totalReceived <= expectedQuantity)
                return ResultDto<int>.Failure("สถานะรับเกิน จำนวนรวมต้องมากกว่าจำนวนที่สั่ง");
        }

        using var conn = connectionFactory.CreateConnection() as System.Data.Common.DbConnection;
        if (conn == null) throw new InvalidOperationException("Could not create DbConnection.");
        await conn.OpenAsync(cancellationToken);
        using var tx = await conn.BeginTransactionAsync(cancellationToken);

        try
        {
            int newId = await shipmentRepository.CreateAsync(dto, currentUserId, tx, cancellationToken);
            await tx.CommitAsync(cancellationToken);
            return ResultDto<int>.Success(newId);
        }
        catch (Exception)
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<ResultDto<bool>> UpdateStatusAsync(int id, string status, int currentUserId, CancellationToken cancellationToken = default)
    {
        using var conn = connectionFactory.CreateConnection() as System.Data.Common.DbConnection;
        if (conn == null) throw new InvalidOperationException("Could not create DbConnection.");
        await conn.OpenAsync(cancellationToken);
        using var tx = await conn.BeginTransactionAsync(cancellationToken);

        try
        {
            var shipmentInfo = await shipmentRepository.GetShipmentInfoAsync(id, tx, cancellationToken);
            if (shipmentInfo == default || shipmentInfo.Status != "Draft")
            {
                await tx.RollbackAsync(cancellationToken);
                return ResultDto<bool>.Failure("อัปเดตสถานะไม่สำเร็จ หรือเอกสารไม่ได้อยู่ในสถานะ Draft");
            }

            var items = await shipmentRepository.GetShipmentItemsAsync(id, tx, cancellationToken);

            bool updated = await shipmentRepository.UpdateStatusAsync(id, status, currentUserId, tx, cancellationToken);
            if (!updated)
            {
                await tx.RollbackAsync(cancellationToken);
                return ResultDto<bool>.Failure("ไม่สามารถอัปเดตสถานะได้");
            }

            if (status == "Completed")
            {
                await ProcessCompletionLogicAsync(id, shipmentInfo.DeliveryId, shipmentInfo.IsForceCloseOrder, items, currentUserId, shipmentInfo.ReceiptDate, tx, cancellationToken);
            }

            await tx.CommitAsync(cancellationToken);
            return ResultDto<bool>.Success(true);
        }
        catch (Exception)
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<ResultDto<bool>> UpdateAsync(int id, VcbShipmentCreateDto dto, int currentUserId, bool isSuperuser, CancellationToken cancellationToken = default)
    {
        if (dto.Items == null || dto.Items.Count == 0)
            return ResultDto<bool>.Failure("กรุณาระบุรายการสินค้าอย่างน้อย 1 รายการ");

        var groupedItems = dto.Items.GroupBy(i => i.OrderItemId);
        foreach (var group in groupedItems)
        {
            var firstItem = group.First();
            int expectedQuantity = firstItem.ExpectedQuantity;
            int totalReceived = group.Sum(i => i.GoodQuantity + i.DefectiveQuantity);
            string status = firstItem.ReceiptStatus;

            if (status == "Complete" && totalReceived != expectedQuantity)
                return ResultDto<bool>.Failure("สถานะรับครบ จำนวนรวมต้องเท่ากับจำนวนที่สั่ง");
            
            if (status == "Incomplete" && totalReceived >= expectedQuantity)
                return ResultDto<bool>.Failure("สถานะรับไม่ครบ จำนวนรวมต้องน้อยกว่าจำนวนที่สั่ง");

            if (status == "Over" && totalReceived <= expectedQuantity)
                return ResultDto<bool>.Failure("สถานะรับเกิน จำนวนรวมต้องมากกว่าจำนวนที่สั่ง");
        }

        using var conn = connectionFactory.CreateConnection() as System.Data.Common.DbConnection;
        if (conn == null) throw new InvalidOperationException("Could not create DbConnection.");
        await conn.OpenAsync(cancellationToken);
        using var tx = await conn.BeginTransactionAsync(cancellationToken);

        try
        {
            var shipmentInfo = await shipmentRepository.GetShipmentInfoAsync(id, tx, cancellationToken);
            if (shipmentInfo == default || (shipmentInfo.Status != "Draft" && !isSuperuser))
            {
                await tx.RollbackAsync(cancellationToken);
                return ResultDto<bool>.Failure("ไม่พบข้อมูลใบรับสินค้า VCANBUY หรือเอกสารไม่ได้อยู่ในสถานะ Draft และคุณไม่มีสิทธิ์ระดับ Superuser");
            }

            if (shipmentInfo.Status == "Completed")
            {
                var oldItems = await shipmentRepository.GetShipmentItemsAsync(id, tx, cancellationToken);
                await ReverseCompletionLogicAsync(id, oldItems, currentUserId, tx, cancellationToken);
            }

            bool updated = await shipmentRepository.UpdateAsync(id, dto, currentUserId, isSuperuser, tx, cancellationToken);
            if (!updated)
            {
                await tx.RollbackAsync(cancellationToken);
                return ResultDto<bool>.Failure("อัปเดตไม่สำเร็จ");
            }

            if (shipmentInfo.Status == "Completed")
            {
                var newItems = await shipmentRepository.GetShipmentItemsAsync(id, tx, cancellationToken);
                await ProcessCompletionLogicAsync(id, dto.DeliveryId, dto.IsForceCloseOrder, newItems, currentUserId, shipmentInfo.ReceiptDate, tx, cancellationToken);
            }

            await tx.CommitAsync(cancellationToken);
            return ResultDto<bool>.Success(true);
        }
        catch (Exception)
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task ProcessCompletionLogicAsync(int shipmentId, int deliveryId, bool isForceClose, IEnumerable<VcbShipmentItemModel> items, int currentUserId, DateTime receiptDate, IDbTransaction tx, CancellationToken cancellationToken)
    {
        var deliveryItems = await deliveryRepository.GetDeliveryBoxesAsync(deliveryId, tx, cancellationToken);
        var expectedCxBoxes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var json in deliveryItems)
        {
            if (string.IsNullOrEmpty(json)) continue;
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var element in doc.RootElement.EnumerateArray())
                    {
                        if (element.TryGetProperty("boxNo", out var boxNoProp))
                        {
                            var boxNo = boxNoProp.GetString();
                            if (!string.IsNullOrEmpty(boxNo)) expectedCxBoxes.Add(boxNo.Trim());
                        }
                    }
                }
            }
            catch { expectedCxBoxes.Add(json.Trim()); }
        }

        var receivedBoxesRaw = await shipmentRepository.GetReceivedShipmentBoxesAsync(deliveryId, null, tx, cancellationToken);
        var receivedCxBoxes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var boxStr in receivedBoxesRaw)
        {
            if (string.IsNullOrEmpty(boxStr)) continue;
            var parts = boxStr.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts) receivedCxBoxes.Add(part.Trim());
        }

        bool allReceived = expectedCxBoxes.Count > 0 && expectedCxBoxes.All(e => receivedCxBoxes.Contains(e));

        if (isForceClose || allReceived)
        {
            await deliveryRepository.UpdateStatusAsync(deliveryId, "Completed", currentUserId, tx, cancellationToken);
        }

        var orderIds = await shipmentRepository.GetOrderIdsByShipmentIdAsync(shipmentId, tx, cancellationToken);
        if (isForceClose)
        {
            foreach(var orderId in orderIds)
            {
                await orderRepository.UpdateStatusAsync(orderId, "Completed", currentUserId, tx, cancellationToken);
            }
        }
        else if (orderIds.Any())
        {
            var fulfilledIds = await shipmentRepository.GetFulfilledOrderIdsAsync(shipmentId, tx, cancellationToken);
            foreach (var orderId in fulfilledIds)
            {
                await orderRepository.UpdateStatusAsync(orderId, "Completed", currentUserId, tx, cancellationToken);
            }
            
            var pendingIds = orderIds.Except(fulfilledIds).ToList();
            foreach (var orderId in pendingIds)
            {
                await orderRepository.UpdateStatusAsync(orderId, "Processing", currentUserId, tx, cancellationToken);
            }
        }

        int branchId = 1;
        if (orderIds.Any())
        {
            branchId = await shipmentRepository.GetBranchIdByOrderIdAsync(orderIds.First(), tx, cancellationToken);
        }

        foreach (var item in items)
        {
            if (item.GoodQuantity > 0)
            {
                await stockRepository.UpdateStockQuantityAsync(item.VariantId, "IN", "Normal", item.GoodQuantity, tx);
                await stockRepository.CreateTransactionAsync(new StockTransactionModel
                {
                    VariantId = item.VariantId,
                    BranchId = branchId,
                    TransactionType = "IN",
                    Condition = "Normal",
                    Quantity = item.GoodQuantity,
                    UnitCost = 0,
                    Notes = $"รับสินค้าจาก VCANBUY Shipment #{shipmentId}",
                    CreatedAt = receiptDate,
                    CreatedBy = currentUserId
                }, tx);
            }
            if (item.DefectiveQuantity > 0)
            {
                await stockRepository.UpdateStockQuantityAsync(item.VariantId, "IN", "Defect", item.DefectiveQuantity, tx);
                await stockRepository.CreateTransactionAsync(new StockTransactionModel
                {
                    VariantId = item.VariantId,
                    BranchId = branchId,
                    TransactionType = "IN",
                    Condition = "Defect",
                    Quantity = item.DefectiveQuantity,
                    UnitCost = 0,
                    Notes = $"รับสินค้าตำหนิจาก VCANBUY Shipment #{shipmentId}",
                    CreatedAt = receiptDate,
                    CreatedBy = currentUserId
                }, tx);
            }
            if (item.RefundAmount > 0)
            {
                await accountTransactionRepo.CreateAsync(new AccountTransactionModel
                {
                    TransactionDate = DateTime.Now,
                    Type = "Income",
                    Amount = item.RefundAmount,
                    ReferenceType = "VcbShipmentRefund",
                    ReferenceId = shipmentId,
                    Notes = $"คืนเงินจาก VCANBUY รายการที่ {item.VariantId}",
                    CreatedBy = currentUserId
                }, tx, cancellationToken);
            }
        }
    }

    private async Task ReverseCompletionLogicAsync(int shipmentId, IEnumerable<VcbShipmentItemModel> items, int currentUserId, IDbTransaction tx, CancellationToken cancellationToken)
    {
        var orderIds = await shipmentRepository.GetOrderIdsByShipmentIdAsync(shipmentId, tx, cancellationToken);
        int branchId = 1;
        if (orderIds.Any())
        {
            branchId = await shipmentRepository.GetBranchIdByOrderIdAsync(orderIds.First(), tx, cancellationToken);
        }

        foreach (var item in items)
        {
            if (item.GoodQuantity > 0)
            {
                await stockRepository.UpdateStockQuantityAsync(item.VariantId, "OUT", "Normal", -item.GoodQuantity, tx);
                await stockRepository.CreateTransactionAsync(new StockTransactionModel
                {
                    VariantId = item.VariantId,
                    BranchId = branchId,
                    TransactionType = "OUT",
                    Condition = "Normal",
                    Quantity = item.GoodQuantity,
                    UnitCost = 0,
                    Notes = $"ลดคลังจากการแก้ไข/ปรับปรุงใบรับสินค้า VCANBUY #{shipmentId}",
                    CreatedBy = currentUserId
                }, tx);
            }
            if (item.DefectiveQuantity > 0)
            {
                await stockRepository.UpdateStockQuantityAsync(item.VariantId, "OUT", "Defect", -item.DefectiveQuantity, tx);
                await stockRepository.CreateTransactionAsync(new StockTransactionModel
                {
                    VariantId = item.VariantId,
                    BranchId = branchId,
                    TransactionType = "OUT",
                    Condition = "Defect",
                    Quantity = item.DefectiveQuantity,
                    UnitCost = 0,
                    Notes = $"ลดคลังสินค้าตำหนิจากการแก้ไข/ปรับปรุงใบรับสินค้า VCANBUY #{shipmentId}",
                    CreatedBy = currentUserId
                }, tx);
            }
        }
        
        var conn = tx.Connection;
        await Dapper.SqlMapper.ExecuteAsync(conn, "DELETE FROM dbo.AccountTransactions WHERE ReferenceType = 'VcbShipmentRefund' AND ReferenceId = @ShipmentId;", new { ShipmentId = shipmentId }, transaction: tx);
    }
}


