using Microsoft.Data.SqlClient;
using PCS_API.DTOs;
using PCS_API.Models;
using PCS_API.Repositories;
using System.Data;

namespace PCS_API.Services;

public class StockService : IStockService
{
    private readonly IStockRepository _repo;
    private readonly ISqlConnectionFactory _connectionFactory;

    public StockService(IStockRepository repo, ISqlConnectionFactory connectionFactory)
    {
        _repo = repo;
        _connectionFactory = connectionFactory;
    }

    public async Task<PagedResultDto<StockDto>> GetStockStatusAsync(StockSearchDto search, CancellationToken cancellationToken = default)
    {
        return await _repo.GetStocksPagedAsync(search, cancellationToken);
    }

    public async Task<PagedResultDto<StockTransactionHistoryDto>> GetTransactionsAsync(string? transactionType, PaginationParamsDto @params, CancellationToken cancellationToken = default)
    {
        return await _repo.GetTransactionsAsync(transactionType, @params, cancellationToken);
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
            Quantity = dto.Quantity,
            UnitCost = dto.UnitCost,
            ReferenceDoc = dto.ReferenceDoc,
            Notes = dto.Notes,
            CreatedBy = userId,
            RequestId = dto.RequestId,
            BranchId = dto.BranchId
        };

        using var conn = _connectionFactory.CreateConnection() as System.Data.Common.DbConnection;
        if (conn == null) throw new InvalidOperationException("Could not create DbConnection.");
        await conn.OpenAsync(cancellationToken);

        using var dbTransaction = await conn.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        try
        {
            bool alreadyProcessed = await _repo.TransactionExistsByRequestIdAsync(dto.RequestId, dbTransaction);
            if (alreadyProcessed) return true;

            bool variantExists = await _repo.VariantExistsAsync(dto.VariantId, dbTransaction);
            if (!variantExists)
            {
                throw new KeyNotFoundException("ไม่พบข้อมูลสินค้า (Product Variant) ที่ระบุ");
            }

            var (before, after) = await _repo.UpdateStockQuantityAsync(dto.VariantId, dto.TransactionType, qtyChange, dbTransaction);

            tx.QuantityBefore = before;
            tx.QuantityAfter = after;
            
            tx.Quantity = after - before;

            await _repo.CreateTransactionAsync(tx, dbTransaction);
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
