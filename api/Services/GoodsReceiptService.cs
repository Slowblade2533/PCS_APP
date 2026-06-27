using PCS_API.DTOs;
using PCS_API.Repositories;
using System.Data.Common;

namespace PCS_API.Services;

public class GoodsReceiptService(
    IGoodsReceiptRepository grRepo, 
    IStockRepository stockRepo, 
    ISqlConnectionFactory connectionFactory,
    IFinancialTransactionService financialTransactionService,
    IAccountTransactionRepository accountTransactionRepo) : IGoodsReceiptService
{
    public async Task<PagedResultDto<GoodsReceiptListDto>> GetPagedAsync(GoodsReceiptSearchDto search, CancellationToken cancellationToken = default)
    {
        return await grRepo.GetPagedAsync(search, cancellationToken);
    }

    public async Task<GoodsReceiptDetailDto?> GetByIdAsync(int receiptId, CancellationToken cancellationToken = default)
    {
        return await grRepo.GetByIdAsync(receiptId, cancellationToken);
    }

    public async Task<ResultDto<int>> CreateAsync(GoodsReceiptCreateDto dto, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection() as DbConnection;
        if (connection == null) throw new InvalidOperationException("Cannot create DbConnection.");
        await connection.OpenAsync(cancellationToken);
        using var tx = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var po = await grRepo.CheckPurchaseOrderStatusAsync(dto.PurchaseOrderId, tx);
            if (po == null) return ResultDto<int>.Failure("ไม่พบใบสั่งซื้อที่อ้างอิง");
            if (po.Value.Status == "CANCELLED") return ResultDto<int>.Failure("ไม่สามารถรับสินค้าสำหรับใบสั่งซื้อที่ยกเลิกแล้ว");

            int receiptId = await grRepo.InsertReceiptAndItemsAsync(dto, tx);

            await tx.CommitAsync(cancellationToken);
            return ResultDto<int>.Success(receiptId);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<ResultDto<bool>> CompleteReceiptAsync(int receiptId, int? updatedBy, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection() as DbConnection;
        if (connection == null) throw new InvalidOperationException("Cannot create DbConnection.");
        await connection.OpenAsync(cancellationToken);
        using var tx = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var receipt = await grRepo.GetBasicInfoAsync(receiptId, tx);
            if (receipt == null) return ResultDto<bool>.Failure("ไม่พบใบรับสินค้า");
            if (receipt.Status == "COMPLETED") return ResultDto<bool>.Failure("ใบรับสินค้านี้ดำเนินการเสร็จสิ้นแล้ว");

            var items = await grRepo.GetItemsAsync(receiptId, tx);

            var dbTx = tx as System.Data.IDbTransaction ?? throw new InvalidOperationException();

            foreach (var item in items)
            {
                // Add Normal stock
                if (item.ReceivedQuantity > 0)
                {
                    await stockRepo.UpdateStockQuantityAsync(item.VariantId, "IN", "Normal", item.ReceivedQuantity, dbTx);
                    await stockRepo.CreateTransactionAsync(new Models.StockTransactionModel { VariantId = item.VariantId, TransactionType = "IN", Condition = "Normal", Quantity = item.ReceivedQuantity, ReferenceDoc = $"GR-{receiptId}", Notes = "รับสินค้าจากใบรับสินค้าทั่วไป", CreatedBy = updatedBy, RequestId = Guid.NewGuid(), BranchId = 1 }, dbTx);
                    await grRepo.UpdatePurchaseOrderItemsReceivedQtyAsync(item.POItemId, item.ReceivedQuantity, dbTx);
                }
                // Add Defective stock
                if (item.DefectiveQuantity > 0)
                {
                    await stockRepo.UpdateStockQuantityAsync(item.VariantId, "IN", "Defective", item.DefectiveQuantity, dbTx);
                    await stockRepo.CreateTransactionAsync(new Models.StockTransactionModel { VariantId = item.VariantId, TransactionType = "IN", Condition = "Defective", Quantity = item.DefectiveQuantity, ReferenceDoc = $"GR-{receiptId}", Notes = "รับสินค้าตำหนิจากใบรับสินค้า", CreatedBy = updatedBy, RequestId = Guid.NewGuid(), BranchId = 1 }, dbTx);
                }
                // Add Damaged stock
                if (item.DamagedQuantity > 0)
                {
                    await stockRepo.UpdateStockQuantityAsync(item.VariantId, "IN", "Damaged", item.DamagedQuantity, dbTx);
                    await stockRepo.CreateTransactionAsync(new Models.StockTransactionModel { VariantId = item.VariantId, TransactionType = "IN", Condition = "Damaged", Quantity = item.DamagedQuantity, ReferenceDoc = $"GR-{receiptId}", Notes = "รับสินค้าเสียหายจากใบรับสินค้า", CreatedBy = updatedBy, RequestId = Guid.NewGuid(), BranchId = 1 }, dbTx);
                }
            }

            await grRepo.UpdateReceiptStatusAsync(receiptId, "COMPLETED", updatedBy, tx);

            var poItemsQty = await grRepo.GetPurchaseOrderItemsQtyAsync(receipt.PurchaseOrderId, tx);
            bool fullyReceived = poItemsQty.All(i => i.ReceivedQuantity >= i.Quantity);
            string newPoStatus = fullyReceived ? "RECEIVED" : "PARTIALLY_RECEIVED";
            
            await grRepo.UpdatePurchaseOrderStatusAsync(receipt.PurchaseOrderId, newPoStatus, tx);

            // ─── Finance Integration ───
            decimal goodsCost = items.Sum(i => (i.ReceivedQuantity + i.DefectiveQuantity + i.DamagedQuantity) * i.UnitPrice);
            decimal totalCost = goodsCost + receipt.ShippingCost;

            if (totalCost > 0)
            {
                // 1. Log Expense (AccountTransactions)
                int expenseId = await accountTransactionRepo.CreateAsync(new Models.AccountTransactionModel
                {
                    TransactionDate = DateTime.Now,
                    Type = "Expense",
                    Amount = totalCost,
                    ReferenceType = "GoodsReceipt",
                    ReferenceId = receiptId,
                    Notes = $"รับสินค้าเข้าระบบ ใบรับสินค้าเลขที่ {receipt?.ReceiptId}",
                    CreatedBy = updatedBy
                }, dbTx, cancellationToken);

                // 2. Log Journal (FinancialTransactions)
                var journalDto = new FinancialTransactionCreateDto
                {
                    TransactionDate = DateOnly.FromDateTime(receipt.ReceiptDate == default ? DateTime.Now : receipt.ReceiptDate),
                    TransactionType = "GOODS_RECEIPT",
                    ReferenceType = "GoodsReceipt",
                    ReferenceId = receiptId,
                    Description = $"บันทึกบัญชีรับสินค้า ใบรับสินค้า {receipt?.ReceiptId}",
                    TotalAmount = totalCost,
                    CreatedBy = updatedBy,
                    LedgerEntries = new List<LedgerEntryCreateDto>
                    {
                        new() { AccountId = 4, DebitAmount = goodsCost, CreditAmount = 0, Memo = "ต้นทุนสินค้าเข้าสต๊อก" },
                    }
                };

                if (receipt.ShippingCost > 0)
                {
                    journalDto.LedgerEntries.Add(new() { AccountId = 14, DebitAmount = receipt.ShippingCost, CreditAmount = 0, Memo = "ค่าขนส่งสินค้าเข้า" });
                }

                journalDto.LedgerEntries.Add(new() { AccountId = 2, DebitAmount = 0, CreditAmount = totalCost, Memo = "ลดยอดเงินสด/ธนาคารสำหรับค่าสินค้า" });

                await financialTransactionService.CreateTransactionWithLedgerAsync(journalDto, dbTx, cancellationToken);
            }

            await tx.CommitAsync(cancellationToken);
            return ResultDto<bool>.Success(true);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
