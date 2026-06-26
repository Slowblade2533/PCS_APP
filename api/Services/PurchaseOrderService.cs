using PCS_API.DTOs;
using PCS_API.Repositories;
using System.Data.Common;

namespace PCS_API.Services;

public class PurchaseOrderService(IPurchaseOrderRepository poRepo, ISqlConnectionFactory connectionFactory) : IPurchaseOrderService
{
    public async Task<PagedResultDto<PurchaseOrderListDto>> GetPagedAsync(PurchaseOrderSearchDto search, CancellationToken cancellationToken = default)
    {
        return await poRepo.GetPagedAsync(search, cancellationToken);
    }

    public async Task<PurchaseOrderDetailDto?> GetByIdAsync(int purchaseOrderId, CancellationToken cancellationToken = default)
    {
        return await poRepo.GetByIdAsync(purchaseOrderId, cancellationToken);
    }

    public async Task<ResultDto<int>> CreateAsync(PurchaseOrderCreateDto dto, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection() as DbConnection;
        if (connection == null) throw new InvalidOperationException("Cannot create DbConnection.");
        await connection.OpenAsync(cancellationToken);
        using var tx = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            decimal subTotal = dto.Items.Sum(i => i.UnitPrice * i.Quantity);
            decimal vatAmount = Math.Round((subTotal - dto.DiscountTotal) * dto.VatRate / 100m, 2);
            decimal grandTotal = subTotal - dto.DiscountTotal + dto.ShippingCost + vatAmount;

            int poId = await poRepo.InsertOrderAndItemsAsync(dto, subTotal, vatAmount, grandTotal, tx);

            await tx.CommitAsync(cancellationToken);
            return ResultDto<int>.Success(poId);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<ResultDto<bool>> UpdateStatusAsync(int purchaseOrderId, PurchaseOrderStatusUpdateDto dto, CancellationToken cancellationToken = default)
    {
        int rows = await poRepo.UpdateStatusAsync(purchaseOrderId, dto.Status, dto.UpdatedBy);
        return rows > 0 ? ResultDto<bool>.Success(true) : ResultDto<bool>.Failure("ไม่พบใบสั่งซื้อที่ต้องการแก้ไข");
    }

    public async Task<ResultDto<bool>> UpdateSlipAsync(int purchaseOrderId, string slipUrl, int? updatedBy, CancellationToken cancellationToken = default)
    {
        int rows = await poRepo.UpdateSlipAsync(purchaseOrderId, slipUrl, updatedBy);
        return rows > 0 ? ResultDto<bool>.Success(true) : ResultDto<bool>.Failure("ไม่พบใบสั่งซื้อ");
    }
}
