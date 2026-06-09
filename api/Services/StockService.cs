using Microsoft.Data.SqlClient;
using PCS_API.DTOs;
using PCS_API.Models;
using PCS_API.Repositories;
using System.Data;

namespace PCS_API.Services;

public class StockService : IStockService
{
    private readonly IStockRepository _repo;
    private readonly string _connectionString;

    public StockService(IStockRepository repo, IConfiguration config)
    {
        _repo = repo;
        _connectionString = config.GetConnectionString("DefaultConnection")!;
    }

    public async Task<PagedResultDto<StockDto>> GetStockStatusAsync(StockSearchDto search)
    {
        return await _repo.GetStocksPagedAsync(search);
    }

    public async Task<PagedResultDto<StockTransactionHistoryDto>> GetTransactionsAsync(string? transactionType, PaginationParamsDto @params)
    {
        return await _repo.GetTransactionsAsync(transactionType, @params);
    }

    public async Task<bool> ProcessStockTransactionAsync(CreateStockTransactionDto dto, int userId)
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
            BranchId = dto.BranchId // ผูกข้อมูลรหัสสาขาที่ส่งมาจากฟอร์ม
        };

        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var dbTransaction = await conn.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            bool alreadyProcessed = await _repo.TransactionExistsByRequestIdAsync(dto.RequestId, dbTransaction);
            if (alreadyProcessed) return true;

            bool variantExists = await _repo.VariantExistsAsync(dto.VariantId, dbTransaction);
            if (!variantExists)
            {
                throw new KeyNotFoundException("ไม่พบข้อมูลสินค้า (Product Variant) ที่ระบุ");
            }

            // อัปเดตสต็อกพร้อมรับค่าก่อน-หลังกลับมา
            var (before, after) = await _repo.UpdateStockQuantityAsync(dto.VariantId, dto.TransactionType, qtyChange, dbTransaction);

            // นำยอดสต็อกก่อนและหลังเปลี่ยนผูกเข้ากับ Model Transaction ตัวหลักเพื่อเตรียมเซฟข้อมูลลงประวัติ
            tx.QuantityBefore = before;
            tx.QuantityAfter = after;
            
            // สำหรับ ADJUST ให้บันทึกเฉพาะส่วนต่างลงประวัติ 
            // หรือในทุกกรณี บันทึกส่วนต่างคือวิธีที่ชัวร์ที่สุดว่ายอดเคลื่อนไหวเท่าไหร่
            tx.Quantity = after - before;

            await _repo.CreateTransactionAsync(tx, dbTransaction);
            await dbTransaction.CommitAsync();

            return true;
        }
        catch (Exception)
        {
            await dbTransaction.RollbackAsync();
            throw;
        }
    }
}
