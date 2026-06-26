using PCS_API.DTOs;
using PCS_API.Repositories;
using System.Data.Common;

namespace PCS_API.Services;

public class SalesOrderService(ISalesOrderRepository salesOrderRepo, IStockRepository stockRepo, ISqlConnectionFactory connectionFactory) : ISalesOrderService
{
    public async Task<PagedResultDto<SalesOrderListDto>> GetPagedAsync(SalesOrderSearchDto search, CancellationToken cancellationToken = default)
    {
        return await salesOrderRepo.GetPagedAsync(search, cancellationToken);
    }

    public async Task<SalesOrderDetailDto?> GetByIdAsync(int orderId, CancellationToken cancellationToken = default)
    {
        return await salesOrderRepo.GetByIdAsync(orderId, cancellationToken);
    }

    public async Task<ResultDto<int>> CreateAsync(SalesOrderCreateDto dto, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection() as DbConnection;
        if (connection == null) throw new InvalidOperationException("Cannot create DbConnection.");
        await connection.OpenAsync(cancellationToken);
        using var tx = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            // Calculate totals
            decimal subTotal = dto.Items.Sum(i => (i.UnitPrice - i.Discount) * i.Quantity);
            decimal discountTotal = dto.Items.Sum(i => i.Discount * i.Quantity);
            decimal vatAmount = Math.Round(subTotal * dto.VatRate / 100m, 2);
            decimal grandTotal = subTotal + vatAmount;

            string orderNo = $"SO-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";

            var variantIds = dto.Items.Select(x => x.VariantId).Distinct().ToList();
            var priceMap = await salesOrderRepo.GetVariantBasePricesAsync(variantIds, tx);

            int orderId = await salesOrderRepo.InsertOrderAndItemsAsync(dto, orderNo, subTotal, discountTotal, vatAmount, grandTotal, priceMap, tx);

            await tx.CommitAsync(cancellationToken);
            return ResultDto<int>.Success(orderId);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<ResultDto<bool>> CompleteOrderAsync(int orderId, int? updatedBy, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection() as DbConnection;
        if (connection == null) throw new InvalidOperationException("Cannot create DbConnection.");
        await connection.OpenAsync(cancellationToken);
        using var tx = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var order = await salesOrderRepo.GetBasicInfoAsync(orderId, tx);
            if (order == null) return ResultDto<bool>.Failure("ไม่พบออเดอร์");
            if (order.OrderStatus != "DRAFT") return ResultDto<bool>.Failure("ออเดอร์นี้ไม่อยู่ในสถานะ DRAFT");

            var items = await salesOrderRepo.GetItemsAsync(orderId, tx);

            // 1. Deduct stock for each item
            var dbTx = tx as System.Data.IDbTransaction ?? throw new InvalidOperationException();
            foreach (var item in items)
            {
                await stockRepo.UpdateStockQuantityAsync(item.VariantId, "OUT", item.Condition, -item.Quantity, dbTx);
                await stockRepo.CreateTransactionAsync(new Models.StockTransactionModel
                {
                    VariantId = item.VariantId,
                    TransactionType = "OUT",
                    Condition = item.Condition,
                    Quantity = item.Quantity,
                    ReferenceDoc = $"SalesOrder-{orderId}",
                    Notes = $"ขายออกจากใบขายเลขที่ SO-{orderId}",
                    CreatedBy = updatedBy,
                    RequestId = Guid.NewGuid(),
                    BranchId = 1
                }, dbTx);
            }

            // 2. Update order status
            await salesOrderRepo.UpdateStatusAsync(orderId, "COMPLETED", updatedBy, tx);

            await tx.CommitAsync(cancellationToken);
            return ResultDto<bool>.Success(true);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<ResultDto<bool>> CancelOrderAsync(int orderId, int? updatedBy, CancellationToken cancellationToken = default)
    {
        var order = await salesOrderRepo.GetBasicInfoAsync(orderId);
        if (order == null) return ResultDto<bool>.Failure("ไม่พบออเดอร์");
        if (order.OrderStatus == "COMPLETED") return ResultDto<bool>.Failure("ไม่สามารถยกเลิกออเดอร์ที่เสร็จสิ้นแล้วได้");

        await salesOrderRepo.UpdateStatusAsync(orderId, "CANCELLED", updatedBy);
        return ResultDto<bool>.Success(true);
    }
}
