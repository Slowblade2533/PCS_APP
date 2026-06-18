using Dapper;
using PCS_API.DTOs;
using PCS_API.Models;
using PCS_API.Services;

namespace PCS_API.Repositories;

/// <summary>
/// Extends stock capabilities: transfer between conditions, stock-take adjust, and scrap.
/// </summary>
public static class StockAdjustmentExtensions
{
    public static async Task<ResultDto<bool>> TransferConditionAsync(
        ISqlConnectionFactory connectionFactory,
        IStockRepository stockRepo,
        StockConditionTransferDto dto)
    {
        using var connection = connectionFactory.CreateConnection() as System.Data.Common.DbConnection;
        if (connection == null) throw new InvalidOperationException("Cannot create DbConnection.");
        await connection.OpenAsync();
        using var tx = await connection.BeginTransactionAsync();

        try
        {
            var dbTx = (System.Data.IDbTransaction)tx;

            // Check source stock
            var source = await connection.QueryFirstOrDefaultAsync<(int CurrentQty, byte[] RowVersion)>(
                "SELECT CurrentQuantity AS CurrentQty, RowVersion FROM dbo.Stocks WHERE VariantId = @VariantId AND Condition = @Condition;",
                new { dto.VariantId, Condition = dto.FromCondition }, tx);

            if (source.CurrentQty < dto.Quantity)
                return ResultDto<bool>.Failure($"สต็อกสภาพ '{dto.FromCondition}' ไม่เพียงพอ (มี {source.CurrentQty} ชิ้น ต้องการโอน {dto.Quantity} ชิ้น)");

            // Deduct from source condition
            await stockRepo.UpdateStockQuantityAsync(dto.VariantId, "OUT", dto.FromCondition, dto.Quantity, dbTx);
            await stockRepo.CreateTransactionAsync(new StockTransactionModel
            {
                VariantId = dto.VariantId,
                TransactionType = "CONDITION_TRANSFER",
                Condition = dto.FromCondition,
                Quantity = dto.Quantity,
                Notes = $"โอนย้ายจากสภาพ '{dto.FromCondition}' → '{dto.ToCondition}': {dto.Reason}",
                CreatedBy = dto.CreatedBy,
                RequestId = Guid.NewGuid(),
                BranchId = 1
            }, dbTx);

            // Add to destination condition
            await stockRepo.UpdateStockQuantityAsync(dto.VariantId, "IN", dto.ToCondition, dto.Quantity, dbTx);
            await stockRepo.CreateTransactionAsync(new StockTransactionModel
            {
                VariantId = dto.VariantId,
                TransactionType = "CONDITION_TRANSFER",
                Condition = dto.ToCondition,
                Quantity = dto.Quantity,
                Notes = $"รับโอนจากสภาพ '{dto.FromCondition}' → '{dto.ToCondition}': {dto.Reason}",
                CreatedBy = dto.CreatedBy,
                RequestId = Guid.NewGuid(),
                BranchId = 1
            }, dbTx);

            await tx.CommitAsync();
            return ResultDto<bool>.Success(true);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public static async Task<ResultDto<bool>> ScrapStockAsync(
        ISqlConnectionFactory connectionFactory,
        IStockRepository stockRepo,
        StockScrapDto dto)
    {
        using var connection = connectionFactory.CreateConnection() as System.Data.Common.DbConnection;
        if (connection == null) throw new InvalidOperationException("Cannot create DbConnection.");
        await connection.OpenAsync();
        using var tx = await connection.BeginTransactionAsync();

        try
        {
            var dbTx = (System.Data.IDbTransaction)tx;

            // Check stock quantity
            var stock = await connection.QueryFirstOrDefaultAsync<(int CurrentQty, byte[] RowVersion)>(
                "SELECT CurrentQuantity AS CurrentQty, RowVersion FROM dbo.Stocks WHERE VariantId = @VariantId AND Condition = @Condition;",
                new { dto.VariantId, dto.Condition }, tx);

            if (stock.CurrentQty < dto.Quantity)
                return ResultDto<bool>.Failure($"สต็อกสภาพ '{dto.Condition}' ไม่เพียงพอ (มี {stock.CurrentQty} ชิ้น ต้องการตัด {dto.Quantity} ชิ้น)");

            string txType = dto.IsSold ? "SELL_SCRAP" : "SCRAP";
            string notes = dto.IsSold
                ? $"ขายซาก {dto.Quantity} ชิ้น ราคา {dto.ScrapPrice:N2} บาท: {dto.Reason}"
                : $"ตัดจำหน่ายทิ้ง {dto.Quantity} ชิ้น: {dto.Reason}";

            await stockRepo.UpdateStockQuantityAsync(dto.VariantId, "OUT", dto.Condition, dto.Quantity, dbTx);
            await stockRepo.CreateTransactionAsync(new StockTransactionModel
            {
                VariantId = dto.VariantId,
                TransactionType = txType,
                Condition = dto.Condition,
                Quantity = dto.Quantity,
                Notes = notes,
                CreatedBy = dto.CreatedBy,
                RequestId = Guid.NewGuid(),
                BranchId = 1
            }, dbTx);

            await tx.CommitAsync();
            return ResultDto<bool>.Success(true);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }
}
