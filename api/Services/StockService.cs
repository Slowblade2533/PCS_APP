using PCS_API.DTOs;
using PCS_API.Models;
using PCS_API.Repositories;
using System.Data;

namespace PCS_API.Services;

public class StockService(IStockRepository repo, ISqlConnectionFactory connectionFactory) : IStockService
{
    public async Task<PagedResultDto<StockDto>> GetStockStatusAsync(StockSearchDto search, CancellationToken cancellationToken = default)
    {
        return await repo.GetStocksPagedAsync(search, cancellationToken);
    }

    public async Task<PagedResultDto<StockTransactionHistoryDto>> GetTransactionsAsync(string? transactionType, PaginationParamsDto @params, CancellationToken cancellationToken = default)
    {
        return await repo.GetTransactionsAsync(transactionType, @params, cancellationToken);
    }

    public async Task<bool> ProcessStockTransactionAsync(CreateStockTransactionDto dto, int userId, CancellationToken cancellationToken = default)
    {
        int qtyChange = dto.TransactionType.ToUpper() switch
        {
            "IN" => Math.Abs(dto.Quantity),
            "OUT" => -Math.Abs(dto.Quantity),
            "DAMAGE" => -Math.Abs(dto.Quantity),
            "LOST" => -Math.Abs(dto.Quantity),
            "RESERVE" => Math.Abs(dto.Quantity),
            "UNRESERVE" => -Math.Abs(dto.Quantity),
            "ADJUST" => dto.Quantity,
            _ => throw new ArgumentException("Invalid Transaction Type")
        };

        var tx = new StockTransactionModel
        {
            VariantId = dto.VariantId,
            TransactionType = dto.TransactionType.ToUpper(),
            Condition = dto.Condition,
            Quantity = dto.Quantity,
            UnitCost = dto.UnitCost,
            ReferenceDoc = dto.ReferenceDoc,
            Notes = dto.Notes,
            CreatedBy = userId,
            RequestId = dto.RequestId,
            BranchId = dto.BranchId
        };

        using var conn = connectionFactory.CreateConnection() as System.Data.Common.DbConnection;
        if (conn == null) throw new InvalidOperationException("Could not create DbConnection.");
        await conn.OpenAsync(cancellationToken);

        using var dbTransaction = await conn.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        try
        {
            bool alreadyProcessed = await repo.TransactionExistsByRequestIdAsync(dto.RequestId, dbTransaction);
            if (alreadyProcessed) return true;

            bool variantExists = await repo.VariantExistsAsync(dto.VariantId, dbTransaction);
            if (!variantExists)
            {
                throw new KeyNotFoundException("ไม่พบข้อมูลสินค้า (Product Variant) ที่ระบุ");
            }

            if (dto.TransactionType.ToUpper() == "DAMAGE" && dto.Condition.ToUpper() != "DAMAGE")
            {
                var condFrom = dto.Condition;
                var condTo = "Damage";

                // Decrement from source condition
                var (beforeFrom, afterFrom) = await repo.UpdateStockQuantityAsync(dto.VariantId, "DAMAGE", condFrom, -Math.Abs(dto.Quantity), dbTransaction);

                // Increment in target condition (Damage)
                var (beforeTo, afterTo) = await repo.UpdateStockQuantityAsync(dto.VariantId, "IN", condTo, Math.Abs(dto.Quantity), dbTransaction);

                // Log decrement transaction
                var txFrom = new StockTransactionModel
                {
                    VariantId = dto.VariantId,
                    TransactionType = "DAMAGE",
                    Condition = condFrom,
                    Quantity = afterFrom - beforeFrom,
                    UnitCost = dto.UnitCost,
                    ReferenceDoc = dto.ReferenceDoc,
                    Notes = dto.Notes ?? $"ย้ายไปสภาพชำรุด {dto.Quantity} ชิ้น",
                    CreatedBy = userId,
                    RequestId = dto.RequestId,
                    BranchId = dto.BranchId,
                    QuantityBefore = beforeFrom,
                    QuantityAfter = afterFrom
                };
                await repo.CreateTransactionAsync(txFrom, dbTransaction);

                // Log increment transaction under Damage
                var txTo = new StockTransactionModel
                {
                    VariantId = dto.VariantId,
                    TransactionType = "IN",
                    Condition = condTo,
                    Quantity = afterTo - beforeTo,
                    UnitCost = dto.UnitCost,
                    ReferenceDoc = dto.ReferenceDoc,
                    Notes = dto.Notes ?? $"ย้ายมาจากสภาพ{condFrom}เนื่องจากชำรุด {dto.Quantity} ชิ้น",
                    CreatedBy = userId,
                    RequestId = Guid.NewGuid(), // Separate RequestId for the auto-move insert
                    BranchId = dto.BranchId,
                    QuantityBefore = beforeTo,
                    QuantityAfter = afterTo
                };
                await repo.CreateTransactionAsync(txTo, dbTransaction);
            }
            else
            {
                var (before, after) = await repo.UpdateStockQuantityAsync(dto.VariantId, dto.TransactionType, dto.Condition, qtyChange, dbTransaction);

                tx.QuantityBefore = before;
                tx.QuantityAfter = after;
                tx.Quantity = after - before;

                await repo.CreateTransactionAsync(tx, dbTransaction);
            }

            await dbTransaction.CommitAsync(cancellationToken);
            return true;
        }
        catch (Exception)
        {
            await dbTransaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}

