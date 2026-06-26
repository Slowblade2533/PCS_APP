using Dapper;
using PCS_API.DTOs;
using System.Data;
using System.Text;

namespace PCS_API.Repositories;

public class GoodsReceiptRepository(ISqlConnectionFactory connectionFactory) : IGoodsReceiptRepository
{
    public async Task<PagedResultDto<GoodsReceiptListDto>> GetPagedAsync(GoodsReceiptSearchDto search, CancellationToken cancellationToken = default)
    {
        using var conn = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        var where = new StringBuilder("WHERE 1=1");

        if (!string.IsNullOrEmpty(search.SearchTerm)) { where.Append(" AND (gr.ReceiptNo LIKE @Search OR po.PONo LIKE @Search OR po.SupplierName LIKE @Search OR gr.TrackingNo LIKE @Search)"); p.Add("Search", $"%{search.SearchTerm}%"); }
        if (search.PurchaseOrderId.HasValue) { where.Append(" AND gr.PurchaseOrderId = @POId"); p.Add("POId", search.PurchaseOrderId.Value); }
        if (!string.IsNullOrEmpty(search.Status)) { where.Append(" AND gr.Status = @Status"); p.Add("Status", search.Status); }

        p.Add("Offset", search.GetSafeOffset());
        p.Add("PageSize", search.PageSize);

        string sql = $@"
            SELECT COUNT(*) FROM dbo.GoodsReceipts gr INNER JOIN dbo.PurchaseOrders po ON gr.PurchaseOrderId = po.PurchaseOrderId {where};
            SELECT gr.ReceiptId, gr.ReceiptNo, po.PONo, po.SupplierName, gr.ReceiptDate,
                   gr.ShippingCompany, gr.TrackingNo, gr.ShippingCost, gr.Status,
                   (SELECT COUNT(*) FROM dbo.GoodsReceiptItems WHERE ReceiptId = gr.ReceiptId) AS ItemCount
            FROM dbo.GoodsReceipts gr
            INNER JOIN dbo.PurchaseOrders po ON gr.PurchaseOrderId = po.PurchaseOrderId
            {where}
            ORDER BY gr.ReceiptDate DESC, gr.ReceiptId DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

        var cmd = new CommandDefinition(sql, p, cancellationToken: cancellationToken);
        using var multi = await conn.QueryMultipleAsync(cmd);
        int total = await multi.ReadSingleAsync<int>();
        var items = await multi.ReadAsync<GoodsReceiptListDto>();
        return new PagedResultDto<GoodsReceiptListDto> { Items = items, TotalCount = total, PageNumber = search.PageNumber, PageSize = search.PageSize };
    }

    public async Task<GoodsReceiptDetailDto?> GetByIdAsync(int receiptId, CancellationToken cancellationToken = default)
    {
        using var conn = connectionFactory.CreateConnection();
        const string sql = @"
            SELECT gr.ReceiptId, gr.ReceiptNo, gr.PurchaseOrderId, po.PONo, po.SupplierName,
                   gr.ReceiptDate, gr.ShippingCompany, gr.TrackingNo, gr.ShippingCost, gr.Status,
                   gr.ShippingPaymentMethod, gr.ShippingPaymentRefNo, gr.ShippingSourceAccount,
                   gr.ShippingSlipUrl, gr.Notes, gr.CreatedAt
            FROM dbo.GoodsReceipts gr
            INNER JOIN dbo.PurchaseOrders po ON gr.PurchaseOrderId = po.PurchaseOrderId
            WHERE gr.ReceiptId = @receiptId;

            SELECT gri.ReceiptItemId, gri.POItemId, gri.VariantId, v.Sku,
                   p.ProductNameTh AS ProductName,
                   COALESCE(NULLIF(v.VariantNameTh,''), NULLIF(LTRIM(RTRIM(CONCAT(v.Color,' ',v.SizeLabel,' ',v.StylePattern))),''), '') AS VariantName,
                   v.ImageUrl, gri.ExpectedQuantity, gri.ReceivedQuantity,
                   gri.DefectiveQuantity, gri.DamagedQuantity
            FROM dbo.GoodsReceiptItems gri
            INNER JOIN dbo.ProductVariants v ON gri.VariantId = v.VariantId
            INNER JOIN dbo.Products p ON v.ProductId = p.ProductId
            WHERE gri.ReceiptId = @receiptId;";

        var cmd = new CommandDefinition(sql, new { receiptId }, cancellationToken: cancellationToken);
        using var multi = await conn.QueryMultipleAsync(cmd);
        var receipt = await multi.ReadFirstOrDefaultAsync<GoodsReceiptDetailDto>();
        if (receipt == null) return null;
        receipt.Items = (await multi.ReadAsync<GoodsReceiptItemDto>()).ToList();
        return receipt;
    }

    public async Task<(int PurchaseOrderId, string Status)?> CheckPurchaseOrderStatusAsync(int poId, IDbTransaction? tx = null)
    {
        var conn = tx?.Connection ?? connectionFactory.CreateConnection();
        try
        {
            return await conn.QueryFirstOrDefaultAsync<(int, string)?>("SELECT PurchaseOrderId, Status FROM dbo.PurchaseOrders WHERE PurchaseOrderId = @poId;", new { poId }, tx);
        }
        finally
        {
            if (tx == null) conn.Dispose();
        }
    }

    public async Task<int> InsertReceiptAndItemsAsync(GoodsReceiptCreateDto dto, IDbTransaction tx)
    {
        const string grSql = @"
            INSERT INTO dbo.GoodsReceipts
                (ReceiptNo, PurchaseOrderId, ReceiptDate, ShippingCompany, TrackingNo,
                 ShippingCost, Status, ShippingPaymentMethod, ShippingPaymentRefNo,
                 ShippingSourceAccount, ShippingSlipUrl, Notes, CreatedBy)
            OUTPUT INSERTED.ReceiptId
            VALUES (@ReceiptNo, @PurchaseOrderId, @ReceiptDate, @ShippingCompany, @TrackingNo,
                    @ShippingCost, 'PENDING', @ShippingPaymentMethod, @ShippingPaymentRefNo,
                    @ShippingSourceAccount, @ShippingSlipUrl, @Notes, @CreatedBy);";

        int receiptId = await tx.Connection.QuerySingleAsync<int>(grSql, dto, tx);

        var itemBatch = new StringBuilder();
        var itemParams = new DynamicParameters();
        itemParams.Add("ReceiptId", receiptId);
        for (int i = 0; i < dto.Items.Count; i++)
        {
            var item = dto.Items[i];
            itemBatch.AppendLine($@"
                INSERT INTO dbo.GoodsReceiptItems
                    (ReceiptId, POItemId, VariantId, ExpectedQuantity, ReceivedQuantity, DefectiveQuantity, DamagedQuantity)
                VALUES (@ReceiptId, @PI{i}, @VI{i}, @Exp{i}, @Rcv{i}, @Def{i}, @Dmg{i});");
            itemParams.Add($"PI{i}", item.POItemId);
            itemParams.Add($"VI{i}", item.VariantId);
            itemParams.Add($"Exp{i}", item.ExpectedQuantity);
            itemParams.Add($"Rcv{i}", item.ReceivedQuantity);
            itemParams.Add($"Def{i}", item.DefectiveQuantity);
            itemParams.Add($"Dmg{i}", item.DamagedQuantity);
        }
        await tx.Connection.ExecuteAsync(itemBatch.ToString(), itemParams, tx);

        return receiptId;
    }

    public async Task<GoodsReceiptDetailDto?> GetBasicInfoAsync(int receiptId, IDbTransaction? tx = null)
    {
        var conn = tx?.Connection ?? connectionFactory.CreateConnection();
        try
        {
            return await conn.QueryFirstOrDefaultAsync<GoodsReceiptDetailDto>(
                "SELECT ReceiptId, Status, PurchaseOrderId FROM dbo.GoodsReceipts WHERE ReceiptId = @receiptId;",
                new { receiptId }, tx);
        }
        finally
        {
            if (tx == null) conn.Dispose();
        }
    }

    public async Task<List<GoodsReceiptItemDto>> GetItemsAsync(int receiptId, IDbTransaction? tx = null)
    {
        var conn = tx?.Connection ?? connectionFactory.CreateConnection();
        try
        {
            var items = await conn.QueryAsync<GoodsReceiptItemDto>(
                "SELECT VariantId, ReceivedQuantity, DefectiveQuantity, DamagedQuantity, POItemId FROM dbo.GoodsReceiptItems WHERE ReceiptId = @receiptId;",
                new { receiptId }, tx);
            return items.ToList();
        }
        finally
        {
            if (tx == null) conn.Dispose();
        }
    }

    public async Task UpdatePurchaseOrderItemsReceivedQtyAsync(long poItemId, int qty, IDbTransaction tx)
    {
        await tx.Connection.ExecuteAsync("UPDATE dbo.PurchaseOrderItems SET ReceivedQuantity = ReceivedQuantity + @qty WHERE POItemId = @poItemId;", new { qty, poItemId }, tx);
    }

    public async Task UpdateReceiptStatusAsync(int receiptId, string status, int? updatedBy, IDbTransaction tx)
    {
        await tx.Connection.ExecuteAsync("UPDATE dbo.GoodsReceipts SET Status = @status, UpdatedBy = @updatedBy, UpdatedAt = GETDATE() WHERE ReceiptId = @receiptId;", new { receiptId, status, updatedBy }, tx);
    }

    public async Task<List<(int Quantity, int ReceivedQuantity)>> GetPurchaseOrderItemsQtyAsync(int poId, IDbTransaction tx)
    {
        var items = await tx.Connection.QueryAsync<(int Quantity, int ReceivedQuantity)>(
            "SELECT Quantity, ReceivedQuantity FROM dbo.PurchaseOrderItems WHERE PurchaseOrderId = @poId;",
            new { poId }, tx);
        return items.ToList();
    }

    public async Task UpdatePurchaseOrderStatusAsync(int poId, string status, IDbTransaction tx)
    {
        await tx.Connection.ExecuteAsync("UPDATE dbo.PurchaseOrders SET Status = @status, UpdatedAt = GETDATE() WHERE PurchaseOrderId = @poId;", new { status, poId }, tx);
    }
}
