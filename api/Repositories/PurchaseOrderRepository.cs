using Dapper;
using PCS_API.DTOs;
using System.Data;
using System.Text;

namespace PCS_API.Repositories;

public class PurchaseOrderRepository(ISqlConnectionFactory connectionFactory) : IPurchaseOrderRepository
{
    public async Task<PagedResultDto<PurchaseOrderListDto>> GetPagedAsync(PurchaseOrderSearchDto search, CancellationToken cancellationToken = default)
    {
        using var conn = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        var where = new StringBuilder("WHERE 1=1");

        if (!string.IsNullOrEmpty(search.SearchTerm)) { where.Append(" AND (po.PONo LIKE @Search OR po.SupplierName LIKE @Search)"); p.Add("Search", $"%{search.SearchTerm}%"); }
        if (!string.IsNullOrEmpty(search.Status)) { where.Append(" AND po.Status = @Status"); p.Add("Status", search.Status); }
        if (search.DateFrom.HasValue) { where.Append(" AND po.PODate >= @DateFrom"); p.Add("DateFrom", search.DateFrom.Value); }
        if (search.DateTo.HasValue) { where.Append(" AND po.PODate <= @DateTo"); p.Add("DateTo", search.DateTo.Value); }

        p.Add("Offset", search.GetSafeOffset());
        p.Add("PageSize", search.PageSize);

        string sql = $@"
            SELECT COUNT(*) FROM dbo.PurchaseOrders po {where};
            SELECT po.PurchaseOrderId, po.PONo, po.PODate, po.SupplierName, po.GrandTotal,
                   po.Status, po.PaymentMethod,
                   (SELECT COUNT(*) FROM dbo.PurchaseOrderItems WHERE PurchaseOrderId = po.PurchaseOrderId) AS ItemCount,
                   ISNULL((SELECT SUM(Quantity) FROM dbo.PurchaseOrderItems WHERE PurchaseOrderId = po.PurchaseOrderId), 0) AS TotalOrdered,
                   ISNULL((SELECT SUM(ReceivedQuantity) FROM dbo.PurchaseOrderItems WHERE PurchaseOrderId = po.PurchaseOrderId), 0) AS TotalReceived
            FROM dbo.PurchaseOrders po
            {where}
            ORDER BY po.PODate DESC, po.PurchaseOrderId DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

        var cmd = new CommandDefinition(sql, p, cancellationToken: cancellationToken);
        using var multi = await conn.QueryMultipleAsync(cmd);
        int total = await multi.ReadSingleAsync<int>();
        var items = await multi.ReadAsync<PurchaseOrderListDto>();
        return new PagedResultDto<PurchaseOrderListDto> { Items = items, TotalCount = total, PageNumber = search.PageNumber, PageSize = search.PageSize };
    }

    public async Task<PurchaseOrderDetailDto?> GetByIdAsync(int purchaseOrderId, CancellationToken cancellationToken = default)
    {
        using var conn = connectionFactory.CreateConnection();
        const string sql = @"
            SELECT po.PurchaseOrderId, po.PONo, po.PODate, po.SupplierName, po.SupplierPhone,
                   po.SupplierTaxId, po.SupplierAddress, po.SubTotal, po.DiscountTotal,
                   po.ShippingCost, po.VatRate, po.VatAmount, po.GrandTotal, po.Status,
                   po.TaxInvoiceId, po.ExpectedDeliveryDate, po.PaymentMethod, po.PaymentRefNo,
                   po.SourceAccountInfo, po.SlipAttachmentUrl, po.Notes, po.CreatedAt,
                   ca.AccountName AS ReceiverAccountName
            FROM dbo.PurchaseOrders po
            LEFT JOIN dbo.ChartOfAccounts ca ON po.ReceiverAccountId = ca.AccountId
            WHERE po.PurchaseOrderId = @purchaseOrderId;

            SELECT poi.POItemId, poi.VariantId, v.Sku, p.ProductNameTh AS ProductName,
                   COALESCE(NULLIF(v.VariantNameTh,''), NULLIF(LTRIM(RTRIM(CONCAT(v.Color,' ',v.SizeLabel,' ',v.StylePattern))),''), '') AS VariantName,
                   v.ImageUrl, poi.Quantity, poi.UnitPrice, poi.LineTotal, poi.ReceivedQuantity
            FROM dbo.PurchaseOrderItems poi
            INNER JOIN dbo.ProductVariants v ON poi.VariantId = v.VariantId
            INNER JOIN dbo.Products p ON v.ProductId = p.ProductId
            WHERE poi.PurchaseOrderId = @purchaseOrderId;";

        var cmd = new CommandDefinition(sql, new { purchaseOrderId }, cancellationToken: cancellationToken);
        using var multi = await conn.QueryMultipleAsync(cmd);
        var po = await multi.ReadFirstOrDefaultAsync<PurchaseOrderDetailDto>();
        if (po == null) return null;
        po.Items = (await multi.ReadAsync<PurchaseOrderItemDto>()).ToList();
        return po;
    }

    public async Task<int> InsertOrderAndItemsAsync(PurchaseOrderCreateDto dto, decimal subTotal, decimal vatAmount, decimal grandTotal, IDbTransaction tx)
    {
        const string poSql = @"
            INSERT INTO dbo.PurchaseOrders
                (PONo, PODate, SupplierName, SupplierPhone, SupplierTaxId, SupplierAddress,
                 SubTotal, DiscountTotal, ShippingCost, VatRate, VatAmount, GrandTotal,
                 Status, ExpectedDeliveryDate, PaymentMethod, PaymentRefNo,
                 SourceAccountInfo, ReceiverAccountId, SlipAttachmentUrl, Notes, CreatedBy)
            OUTPUT INSERTED.PurchaseOrderId
            VALUES (@PONo, @PODate, @SupplierName, @SupplierPhone, @SupplierTaxId, @SupplierAddress,
                    @SubTotal, @DiscountTotal, @ShippingCost, @VatRate, @VatAmount, @GrandTotal,
                    'ORDERED', @ExpectedDeliveryDate, @PaymentMethod, @PaymentRefNo,
                    @SourceAccountInfo, @ReceiverAccountId, @SlipAttachmentUrl, @Notes, @CreatedBy);";

        var poParams = new DynamicParameters(dto);
        poParams.Add("SubTotal", subTotal);
        poParams.Add("VatAmount", vatAmount);
        poParams.Add("GrandTotal", grandTotal);

        int poId = await tx.Connection.QuerySingleAsync<int>(poSql, poParams, tx);

        var itemBatch = new StringBuilder();
        var itemParams = new DynamicParameters();
        itemParams.Add("POId", poId);
        for (int i = 0; i < dto.Items.Count; i++)
        {
            var item = dto.Items[i];
            itemBatch.AppendLine($@"
                INSERT INTO dbo.PurchaseOrderItems (PurchaseOrderId, VariantId, Quantity, UnitPrice, LineTotal)
                VALUES (@POId, @VI{i}, @Qty{i}, @UP{i}, @LT{i});");
            itemParams.Add($"VI{i}", item.VariantId);
            itemParams.Add($"Qty{i}", item.Quantity);
            itemParams.Add($"UP{i}", item.UnitPrice);
            itemParams.Add($"LT{i}", item.UnitPrice * item.Quantity);
        }
        await tx.Connection.ExecuteAsync(itemBatch.ToString(), itemParams, tx);

        return poId;
    }

    public async Task<int> UpdateStatusAsync(int purchaseOrderId, string status, int? updatedBy, IDbTransaction? tx = null)
    {
        var conn = tx?.Connection ?? connectionFactory.CreateConnection();
        try
        {
            return await conn.ExecuteAsync(
                "UPDATE dbo.PurchaseOrders SET Status = @status, UpdatedBy = @updatedBy, UpdatedAt = GETDATE() WHERE PurchaseOrderId = @purchaseOrderId;",
                new { purchaseOrderId, status, updatedBy }, tx);
        }
        finally
        {
            if (tx == null) conn.Dispose();
        }
    }

    public async Task<int> UpdateSlipAsync(int purchaseOrderId, string slipUrl, int? updatedBy, IDbTransaction? tx = null)
    {
        var conn = tx?.Connection ?? connectionFactory.CreateConnection();
        try
        {
            return await conn.ExecuteAsync(
                "UPDATE dbo.PurchaseOrders SET SlipAttachmentUrl = @slipUrl, UpdatedBy = @updatedBy, UpdatedAt = GETDATE() WHERE PurchaseOrderId = @purchaseOrderId;",
                new { purchaseOrderId, slipUrl, updatedBy }, tx);
        }
        finally
        {
            if (tx == null) conn.Dispose();
        }
    }
}
