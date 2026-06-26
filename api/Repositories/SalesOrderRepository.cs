using Dapper;
using PCS_API.DTOs;
using System.Data;
using System.Text;

namespace PCS_API.Repositories;

public class SalesOrderRepository(ISqlConnectionFactory connectionFactory) : ISalesOrderRepository
{
    public async Task<PagedResultDto<SalesOrderListDto>> GetPagedAsync(SalesOrderSearchDto search, CancellationToken cancellationToken = default)
    {
        using var conn = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        var where = new StringBuilder("WHERE 1=1");

        if (!string.IsNullOrEmpty(search.SearchTerm))
        {
            where.Append(" AND (so.OrderNo LIKE @Search OR so.CustomerName LIKE @Search)");
            p.Add("Search", $"%{search.SearchTerm}%");
        }
        if (!string.IsNullOrEmpty(search.Status)) { where.Append(" AND so.OrderStatus = @Status"); p.Add("Status", search.Status); }
        if (search.DateFrom.HasValue) { where.Append(" AND so.OrderDate >= @DateFrom"); p.Add("DateFrom", search.DateFrom.Value); }
        if (search.DateTo.HasValue) { where.Append(" AND so.OrderDate <= @DateTo"); p.Add("DateTo", search.DateTo.Value); }

        p.Add("Offset", search.GetSafeOffset());
        p.Add("PageSize", search.PageSize);

        string sql = $@"
            SELECT COUNT(*) FROM dbo.SalesOrders so {where};
            SELECT so.OrderId, so.OrderNo, so.OrderDate, so.CustomerName,
                   so.GrandTotal, so.OrderStatus, so.PaymentMethod,
                   (SELECT COUNT(*) FROM dbo.SalesOrderItems WHERE OrderId = so.OrderId) AS ItemCount
            FROM dbo.SalesOrders so
            {where}
            ORDER BY so.OrderDate DESC, so.OrderId DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

        var cmd = new CommandDefinition(sql, p, cancellationToken: cancellationToken);
        using var multi = await conn.QueryMultipleAsync(cmd);
        int total = await multi.ReadSingleAsync<int>();
        var items = await multi.ReadAsync<SalesOrderListDto>();
        return new PagedResultDto<SalesOrderListDto> { Items = items, TotalCount = total, PageNumber = search.PageNumber, PageSize = search.PageSize };
    }

    public async Task<SalesOrderDetailDto?> GetByIdAsync(int orderId, CancellationToken cancellationToken = default)
    {
        using var conn = connectionFactory.CreateConnection();
        const string sql = @"
            SELECT so.OrderId, so.OrderNo, so.OrderDate, so.CustomerName, so.CustomerPhone,
                   so.CustomerTaxId, so.CustomerAddress, so.SubTotal, so.DiscountTotal,
                   so.VatRate, so.VatAmount, so.GrandTotal, so.OrderStatus, so.TaxInvoiceId,
                   so.PaymentMethod, so.PaymentRefNo, so.SlipAttachmentUrl, so.Notes, so.CreatedAt,
                   ca.AccountName AS ReceiverAccountName
            FROM dbo.SalesOrders so
            LEFT JOIN dbo.ChartOfAccounts ca ON so.ReceiverAccountId = ca.AccountId
            WHERE so.OrderId = @orderId;

            SELECT soi.OrderItemId, soi.VariantId, v.Sku, p.ProductNameTh AS ProductName,
                   COALESCE(NULLIF(v.VariantNameTh, ''), NULLIF(LTRIM(RTRIM(CONCAT(v.Color,' ',v.SizeLabel,' ',v.StylePattern))),''), '') AS VariantName,
                   v.ImageUrl, soi.Condition, soi.Quantity, soi.UnitPrice, soi.Discount, soi.LineTotal
            FROM dbo.SalesOrderItems soi
            INNER JOIN dbo.ProductVariants v ON soi.VariantId = v.VariantId
            INNER JOIN dbo.Products p ON v.ProductId = p.ProductId
            WHERE soi.OrderId = @orderId;";

        var cmd = new CommandDefinition(sql, new { orderId }, cancellationToken: cancellationToken);
        using var multi = await conn.QueryMultipleAsync(cmd);
        var order = await multi.ReadFirstOrDefaultAsync<SalesOrderDetailDto>();
        if (order == null) return null;
        order.Items = (await multi.ReadAsync<SalesOrderItemDto>()).ToList();
        return order;
    }

    public async Task<Dictionary<int, decimal>> GetVariantBasePricesAsync(List<int> variantIds, IDbTransaction tx)
    {
        if (variantIds == null || !variantIds.Any()) return new Dictionary<int, decimal>();
        var prices = await tx.Connection.QueryAsync<(int VariantId, decimal BasePrice)>(
            "SELECT VariantId, ISNULL(BasePrice, 0) AS BasePrice FROM dbo.ProductPrices WHERE VariantId IN @VariantIds;",
            new { VariantIds = variantIds }, tx);
        return prices.ToDictionary(x => x.VariantId, x => x.BasePrice);
    }

    public async Task<int> InsertOrderAndItemsAsync(SalesOrderCreateDto dto, string orderNo, decimal subTotal, decimal discountTotal, decimal vatAmount, decimal grandTotal, Dictionary<int, decimal> priceMap, IDbTransaction tx)
    {
        const string orderSql = @"
            INSERT INTO dbo.SalesOrders
                (OrderNo, OrderDate, CustomerName, CustomerPhone, CustomerTaxId, CustomerAddress,
                 SubTotal, DiscountTotal, VatRate, VatAmount, GrandTotal, OrderStatus,
                 PaymentMethod, PaymentRefNo, ReceiverAccountId, SlipAttachmentUrl, Notes, CreatedBy)
            OUTPUT INSERTED.OrderId
            VALUES (@OrderNo, @OrderDate, @CustomerName, @CustomerPhone, @CustomerTaxId, @CustomerAddress,
                    @SubTotal, @DiscountTotal, @VatRate, @VatAmount, @GrandTotal, 'DRAFT',
                    @PaymentMethod, @PaymentRefNo, @ReceiverAccountId, @SlipAttachmentUrl, @Notes, @CreatedBy);";

        var orderParams = new DynamicParameters(dto);
        orderParams.Add("OrderNo", orderNo);
        orderParams.Add("SubTotal", subTotal);
        orderParams.Add("DiscountTotal", discountTotal);
        orderParams.Add("VatAmount", vatAmount);
        orderParams.Add("GrandTotal", grandTotal);

        int orderId = await tx.Connection.QuerySingleAsync<int>(orderSql, orderParams, tx);

        var itemBatch = new StringBuilder();
        var itemParams = new DynamicParameters();
        itemParams.Add("OrderId", orderId);
        for (int i = 0; i < dto.Items.Count; i++)
        {
            var item = dto.Items[i];
            decimal lineTotal = (item.UnitPrice - item.Discount) * item.Quantity;
            decimal unitCost = priceMap.TryGetValue(item.VariantId, out var cost) ? cost : 0m;

            itemBatch.AppendLine($@"
                INSERT INTO dbo.SalesOrderItems
                    (OrderId, VariantId, Condition, Quantity, UnitPrice, Discount, LineTotal, UnitCostAtSale)
                VALUES (@OrderId, @VI{i}, @Cond{i}, @Qty{i}, @UP{i}, @Disc{i}, @LT{i}, @Cost{i});");
            itemParams.Add($"VI{i}", item.VariantId);
            itemParams.Add($"Cond{i}", item.Condition);
            itemParams.Add($"Qty{i}", item.Quantity);
            itemParams.Add($"UP{i}", item.UnitPrice);
            itemParams.Add($"Disc{i}", item.Discount);
            itemParams.Add($"LT{i}", lineTotal);
            itemParams.Add($"Cost{i}", unitCost);
        }
        await tx.Connection.ExecuteAsync(itemBatch.ToString(), itemParams, tx);
        return orderId;
    }

    public async Task<SalesOrderDetailDto?> GetBasicInfoAsync(int orderId, IDbTransaction? tx = null)
    {
        var conn = tx?.Connection ?? connectionFactory.CreateConnection();
        try
        {
            return await conn.QueryFirstOrDefaultAsync<SalesOrderDetailDto>(
                "SELECT OrderId, OrderStatus, GrandTotal, VatAmount, VatRate, ReceiverAccountId FROM dbo.SalesOrders WHERE OrderId = @orderId;",
                new { orderId }, tx);
        }
        finally
        {
            if (tx == null) conn.Dispose();
        }
    }

    public async Task<List<SalesOrderItemDto>> GetItemsAsync(int orderId, IDbTransaction? tx = null)
    {
        var conn = tx?.Connection ?? connectionFactory.CreateConnection();
        try
        {
            var items = await conn.QueryAsync<SalesOrderItemDto>(
                "SELECT VariantId, Condition, Quantity, UnitPrice, Discount, LineTotal FROM dbo.SalesOrderItems WHERE OrderId = @orderId;",
                new { orderId }, tx);
            return items.ToList();
        }
        finally
        {
            if (tx == null) conn.Dispose();
        }
    }

    public async Task UpdateStatusAsync(int orderId, string status, int? updatedBy, IDbTransaction? tx = null)
    {
        var conn = tx?.Connection ?? connectionFactory.CreateConnection();
        try
        {
            await conn.ExecuteAsync(
                "UPDATE dbo.SalesOrders SET OrderStatus = @status, UpdatedBy = @updatedBy, UpdatedAt = GETDATE() WHERE OrderId = @orderId;",
                new { orderId, status, updatedBy }, tx);
        }
        finally
        {
            if (tx == null) conn.Dispose();
        }
    }
}
