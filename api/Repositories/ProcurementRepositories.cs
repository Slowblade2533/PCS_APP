using Dapper;
using PCS_API.DTOs;
using PCS_API.Models;
using PCS_API.Services;
using System.Text;

namespace PCS_API.Repositories;

// ─────────────────────────────────────────────────────────────────────────────
// SALES ORDER REPOSITORY
// ─────────────────────────────────────────────────────────────────────────────
public class SalesOrderRepository : ISalesOrderRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly IStockRepository _stockRepo;
    private readonly IFinancialRepository _financialRepo;

    public SalesOrderRepository(ISqlConnectionFactory connectionFactory, IStockRepository stockRepo, IFinancialRepository financialRepo)
    {
        _connectionFactory = connectionFactory;
        _stockRepo = stockRepo;
        _financialRepo = financialRepo;
    }

    public async Task<PagedResultDto<SalesOrderListDto>> GetPagedAsync(SalesOrderSearchDto search, CancellationToken cancellationToken = default)
    {
        using var conn = _connectionFactory.CreateConnection();
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

    public async Task<SalesOrderDetailDto?> GetByIdAsync(int orderId)
    {
        using var conn = _connectionFactory.CreateConnection();
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

        using var multi = await conn.QueryMultipleAsync(sql, new { orderId });
        var order = await multi.ReadFirstOrDefaultAsync<SalesOrderDetailDto>();
        if (order == null) return null;
        order.Items = (await multi.ReadAsync<SalesOrderItemDto>()).ToList();
        return order;
    }

    public async Task<ResultDto<int>> CreateAsync(SalesOrderCreateDto dto)
    {
        using var connection = _connectionFactory.CreateConnection() as System.Data.Common.DbConnection;
        if (connection == null) throw new InvalidOperationException("Cannot create DbConnection.");
        await connection.OpenAsync();
        using var tx = await connection.BeginTransactionAsync();

        try
        {
            // Calculate totals
            decimal subTotal = dto.Items.Sum(i => (i.UnitPrice - i.Discount) * i.Quantity);
            decimal discountTotal = dto.Items.Sum(i => i.Discount * i.Quantity);
            decimal vatAmount = Math.Round(subTotal * dto.VatRate / 100m, 2);
            decimal grandTotal = subTotal + vatAmount;

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
            orderParams.Add("SubTotal", subTotal);
            orderParams.Add("DiscountTotal", discountTotal);
            orderParams.Add("VatAmount", vatAmount);
            orderParams.Add("GrandTotal", grandTotal);

            // Auto-generate order number if not specified
            if (string.IsNullOrEmpty(dto.Notes))
                orderParams.Add("OrderNo", $"SO-{DateTime.Now:yyyyMMdd-HHmmss}");
            else
                orderParams.Add("OrderNo", $"SO-{DateTime.Now:yyyyMMdd-HHmmss}");

            // Actually generate OrderNo
            string orderNo = $"SO-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
            orderParams = new DynamicParameters(dto);
            orderParams.Add("OrderNo", orderNo);
            orderParams.Add("SubTotal", subTotal);
            orderParams.Add("DiscountTotal", discountTotal);
            orderParams.Add("VatAmount", vatAmount);
            orderParams.Add("GrandTotal", grandTotal);

            int orderId = await connection.QuerySingleAsync<int>(orderSql, orderParams, tx);

            // Fetch unit costs from ProductPrices for COGS tracking in a single batch query
            var variantIds = dto.Items.Select(x => x.VariantId).Distinct().ToList();
            var priceMap = new Dictionary<int, decimal>();
            if (variantIds.Any())
            {
                var prices = await connection.QueryAsync<(int VariantId, decimal BasePrice)>(
                    "SELECT VariantId, ISNULL(BasePrice, 0) AS BasePrice FROM dbo.ProductPrices WHERE VariantId IN @VariantIds;",
                    new { VariantIds = variantIds }, tx);
                priceMap = prices.ToDictionary(x => x.VariantId, x => x.BasePrice);
            }

            // Insert items
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
            await connection.ExecuteAsync(itemBatch.ToString(), itemParams, tx);

            await tx.CommitAsync();
            return ResultDto<int>.Success(orderId);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<ResultDto<bool>> CompleteOrderAsync(int orderId, int? updatedBy)
    {
        using var connection = _connectionFactory.CreateConnection() as System.Data.Common.DbConnection;
        if (connection == null) throw new InvalidOperationException("Cannot create DbConnection.");
        await connection.OpenAsync();
        using var tx = await connection.BeginTransactionAsync();

        try
        {
            // Verify order exists & is DRAFT
            var order = await connection.QueryFirstOrDefaultAsync<SalesOrderDetailDto>(
                "SELECT OrderId, OrderStatus, GrandTotal, VatAmount, VatRate, ReceiverAccountId FROM dbo.SalesOrders WHERE OrderId = @orderId;",
                new { orderId }, tx);
            if (order == null) return ResultDto<bool>.Failure("ไม่พบออเดอร์");
            if (order.OrderStatus != "DRAFT") return ResultDto<bool>.Failure("ออเดอร์นี้ไม่อยู่ในสถานะ DRAFT");

            // Load items
            var items = (await connection.QueryAsync<SalesOrderItemDto>(
                "SELECT VariantId, Condition, Quantity, UnitPrice, Discount, LineTotal FROM dbo.SalesOrderItems WHERE OrderId = @orderId;",
                new { orderId }, tx)).ToList();

            // 1. Deduct stock for each item
            var stockTxModel = new StockTransactionModel();
            foreach (var item in items)
            {
                await _stockRepo.UpdateStockQuantityAsync(item.VariantId, "OUT", item.Condition, -item.Quantity, tx as System.Data.IDbTransaction ?? throw new InvalidOperationException());
                await _stockRepo.CreateTransactionAsync(new StockTransactionModel
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
                }, tx as System.Data.IDbTransaction ?? throw new InvalidOperationException());
            }

            // 2. Update order status
            await connection.ExecuteAsync(
                "UPDATE dbo.SalesOrders SET OrderStatus = 'COMPLETED', UpdatedBy = @updatedBy, UpdatedAt = GETDATE() WHERE OrderId = @orderId;",
                new { orderId, updatedBy }, tx);

            await tx.CommitAsync();
            return ResultDto<bool>.Success(true);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<ResultDto<bool>> CancelOrderAsync(int orderId, int? updatedBy)
    {
        using var conn = _connectionFactory.CreateConnection();
        var existing = await conn.QueryFirstOrDefaultAsync<dynamic>("SELECT OrderStatus FROM dbo.SalesOrders WHERE OrderId = @orderId;", new { orderId });
        if (existing == null) return ResultDto<bool>.Failure("ไม่พบออเดอร์");
        if (existing.OrderStatus == "COMPLETED") return ResultDto<bool>.Failure("ไม่สามารถยกเลิกออเดอร์ที่เสร็จสิ้นแล้วได้");

        await conn.ExecuteAsync(
            "UPDATE dbo.SalesOrders SET OrderStatus = 'CANCELLED', UpdatedBy = @updatedBy, UpdatedAt = GETDATE() WHERE OrderId = @orderId;",
            new { orderId, updatedBy });
        return ResultDto<bool>.Success(true);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// PURCHASE ORDER REPOSITORY
// ─────────────────────────────────────────────────────────────────────────────
public class PurchaseOrderRepository : IPurchaseOrderRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public PurchaseOrderRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<PagedResultDto<PurchaseOrderListDto>> GetPagedAsync(PurchaseOrderSearchDto search, CancellationToken cancellationToken = default)
    {
        using var conn = _connectionFactory.CreateConnection();
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

    public async Task<PurchaseOrderDetailDto?> GetByIdAsync(int purchaseOrderId)
    {
        using var conn = _connectionFactory.CreateConnection();
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

        using var multi = await conn.QueryMultipleAsync(sql, new { purchaseOrderId });
        var po = await multi.ReadFirstOrDefaultAsync<PurchaseOrderDetailDto>();
        if (po == null) return null;
        po.Items = (await multi.ReadAsync<PurchaseOrderItemDto>()).ToList();
        return po;
    }

    public async Task<ResultDto<int>> CreateAsync(PurchaseOrderCreateDto dto)
    {
        using var connection = _connectionFactory.CreateConnection() as System.Data.Common.DbConnection;
        if (connection == null) throw new InvalidOperationException("Cannot create DbConnection.");
        await connection.OpenAsync();
        using var tx = await connection.BeginTransactionAsync();

        try
        {
            decimal subTotal = dto.Items.Sum(i => i.UnitPrice * i.Quantity);
            decimal vatAmount = Math.Round((subTotal - dto.DiscountTotal) * dto.VatRate / 100m, 2);
            decimal grandTotal = subTotal - dto.DiscountTotal + dto.ShippingCost + vatAmount;

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

            int poId = await connection.QuerySingleAsync<int>(poSql, poParams, tx);

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
            await connection.ExecuteAsync(itemBatch.ToString(), itemParams, tx);

            await tx.CommitAsync();
            return ResultDto<int>.Success(poId);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<ResultDto<bool>> UpdateStatusAsync(int purchaseOrderId, PurchaseOrderStatusUpdateDto dto)
    {
        using var conn = _connectionFactory.CreateConnection();
        int rows = await conn.ExecuteAsync(
            "UPDATE dbo.PurchaseOrders SET Status = @Status, UpdatedBy = @UpdatedBy, UpdatedAt = GETDATE() WHERE PurchaseOrderId = @purchaseOrderId;",
            new { purchaseOrderId, dto.Status, dto.UpdatedBy });
        return rows > 0 ? ResultDto<bool>.Success(true) : ResultDto<bool>.Failure("ไม่พบใบสั่งซื้อที่ต้องการแก้ไข");
    }

    public async Task<ResultDto<bool>> UpdateSlipAsync(int purchaseOrderId, string slipUrl, int? updatedBy)
    {
        using var conn = _connectionFactory.CreateConnection();
        int rows = await conn.ExecuteAsync(
            "UPDATE dbo.PurchaseOrders SET SlipAttachmentUrl = @slipUrl, UpdatedBy = @updatedBy, UpdatedAt = GETDATE() WHERE PurchaseOrderId = @purchaseOrderId;",
            new { purchaseOrderId, slipUrl, updatedBy });
        return rows > 0 ? ResultDto<bool>.Success(true) : ResultDto<bool>.Failure("ไม่พบใบสั่งซื้อ");
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// GOODS RECEIPT REPOSITORY
// ─────────────────────────────────────────────────────────────────────────────
public class GoodsReceiptRepository : IGoodsReceiptRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly IStockRepository _stockRepo;

    public GoodsReceiptRepository(ISqlConnectionFactory connectionFactory, IStockRepository stockRepo)
    {
        _connectionFactory = connectionFactory;
        _stockRepo = stockRepo;
    }

    public async Task<PagedResultDto<GoodsReceiptListDto>> GetPagedAsync(GoodsReceiptSearchDto search, CancellationToken cancellationToken = default)
    {
        using var conn = _connectionFactory.CreateConnection();
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

    public async Task<GoodsReceiptDetailDto?> GetByIdAsync(int receiptId)
    {
        using var conn = _connectionFactory.CreateConnection();
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

        using var multi = await conn.QueryMultipleAsync(sql, new { receiptId });
        var receipt = await multi.ReadFirstOrDefaultAsync<GoodsReceiptDetailDto>();
        if (receipt == null) return null;
        receipt.Items = (await multi.ReadAsync<GoodsReceiptItemDto>()).ToList();
        return receipt;
    }

    public async Task<ResultDto<int>> CreateAsync(GoodsReceiptCreateDto dto)
    {
        using var connection = _connectionFactory.CreateConnection() as System.Data.Common.DbConnection;
        if (connection == null) throw new InvalidOperationException("Cannot create DbConnection.");
        await connection.OpenAsync();
        using var tx = await connection.BeginTransactionAsync();

        try
        {
            // Verify PO exists
            var po = await connection.QueryFirstOrDefaultAsync<dynamic>("SELECT PurchaseOrderId, Status FROM dbo.PurchaseOrders WHERE PurchaseOrderId = @Id;", new { Id = dto.PurchaseOrderId }, tx);
            if (po == null) return ResultDto<int>.Failure("ไม่พบใบสั่งซื้อที่อ้างอิง");
            if (po.Status == "CANCELLED") return ResultDto<int>.Failure("ไม่สามารถรับสินค้าสำหรับใบสั่งซื้อที่ยกเลิกแล้ว");

            const string grSql = @"
                INSERT INTO dbo.GoodsReceipts
                    (ReceiptNo, PurchaseOrderId, ReceiptDate, ShippingCompany, TrackingNo,
                     ShippingCost, Status, ShippingPaymentMethod, ShippingPaymentRefNo,
                     ShippingSourceAccount, ShippingSlipUrl, Notes, CreatedBy)
                OUTPUT INSERTED.ReceiptId
                VALUES (@ReceiptNo, @PurchaseOrderId, @ReceiptDate, @ShippingCompany, @TrackingNo,
                        @ShippingCost, 'PENDING', @ShippingPaymentMethod, @ShippingPaymentRefNo,
                        @ShippingSourceAccount, @ShippingSlipUrl, @Notes, @CreatedBy);";

            int receiptId = await connection.QuerySingleAsync<int>(grSql, dto, tx);

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
            await connection.ExecuteAsync(itemBatch.ToString(), itemParams, tx);

            await tx.CommitAsync();
            return ResultDto<int>.Success(receiptId);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<ResultDto<bool>> CompleteReceiptAsync(int receiptId, int? updatedBy)
    {
        using var connection = _connectionFactory.CreateConnection() as System.Data.Common.DbConnection;
        if (connection == null) throw new InvalidOperationException("Cannot create DbConnection.");
        await connection.OpenAsync();
        using var tx = await connection.BeginTransactionAsync();

        try
        {
            var receipt = await connection.QueryFirstOrDefaultAsync<GoodsReceiptDetailDto>(
                "SELECT ReceiptId, Status, PurchaseOrderId FROM dbo.GoodsReceipts WHERE ReceiptId = @receiptId;",
                new { receiptId }, tx);
            if (receipt == null) return ResultDto<bool>.Failure("ไม่พบใบรับสินค้า");
            if (receipt.Status == "COMPLETED") return ResultDto<bool>.Failure("ใบรับสินค้านี้ดำเนินการเสร็จสิ้นแล้ว");

            var items = (await connection.QueryAsync<GoodsReceiptItemDto>(
                "SELECT VariantId, ReceivedQuantity, DefectiveQuantity, DamagedQuantity, POItemId FROM dbo.GoodsReceiptItems WHERE ReceiptId = @receiptId;",
                new { receiptId }, tx)).ToList();

            var dbTx = tx as System.Data.IDbTransaction ?? throw new InvalidOperationException();

            foreach (var item in items)
            {
                // Add Normal stock
                if (item.ReceivedQuantity > 0)
                {
                    await _stockRepo.UpdateStockQuantityAsync(item.VariantId, "IN", "Normal", item.ReceivedQuantity, dbTx);
                    await _stockRepo.CreateTransactionAsync(new StockTransactionModel { VariantId = item.VariantId, TransactionType = "IN", Condition = "Normal", Quantity = item.ReceivedQuantity, ReferenceDoc = $"GR-{receiptId}", Notes = "รับสินค้าจากใบรับสินค้าทั่วไป", CreatedBy = updatedBy, RequestId = Guid.NewGuid(), BranchId = 1 }, dbTx);
                    // Update ReceivedQuantity in PO item
                    await connection.ExecuteAsync("UPDATE dbo.PurchaseOrderItems SET ReceivedQuantity = ReceivedQuantity + @qty WHERE POItemId = @poItemId;", new { qty = item.ReceivedQuantity, poItemId = item.POItemId }, tx);
                }
                // Add Defective stock
                if (item.DefectiveQuantity > 0)
                {
                    await _stockRepo.UpdateStockQuantityAsync(item.VariantId, "IN", "Defective", item.DefectiveQuantity, dbTx);
                    await _stockRepo.CreateTransactionAsync(new StockTransactionModel { VariantId = item.VariantId, TransactionType = "IN", Condition = "Defective", Quantity = item.DefectiveQuantity, ReferenceDoc = $"GR-{receiptId}", Notes = "รับสินค้าตำหนิจากใบรับสินค้า", CreatedBy = updatedBy, RequestId = Guid.NewGuid(), BranchId = 1 }, dbTx);
                }
                // Add Damaged stock
                if (item.DamagedQuantity > 0)
                {
                    await _stockRepo.UpdateStockQuantityAsync(item.VariantId, "IN", "Damaged", item.DamagedQuantity, dbTx);
                    await _stockRepo.CreateTransactionAsync(new StockTransactionModel { VariantId = item.VariantId, TransactionType = "IN", Condition = "Damaged", Quantity = item.DamagedQuantity, ReferenceDoc = $"GR-{receiptId}", Notes = "รับสินค้าเสียหายจากใบรับสินค้า", CreatedBy = updatedBy, RequestId = Guid.NewGuid(), BranchId = 1 }, dbTx);
                }
            }

            // Mark receipt as COMPLETED
            await connection.ExecuteAsync("UPDATE dbo.GoodsReceipts SET Status = 'COMPLETED', UpdatedBy = @updatedBy, UpdatedAt = GETDATE() WHERE ReceiptId = @receiptId;", new { receiptId, updatedBy }, tx);

            // Update PO Status to PARTIALLY_RECEIVED or RECEIVED
            var poItems = await connection.QueryAsync<(int Quantity, int ReceivedQuantity)>(
                "SELECT Quantity, ReceivedQuantity FROM dbo.PurchaseOrderItems WHERE PurchaseOrderId = @poId;",
                new { poId = receipt.PurchaseOrderId }, tx);
            bool fullyReceived = poItems.All(i => i.ReceivedQuantity >= i.Quantity);
            string newPoStatus = fullyReceived ? "RECEIVED" : "PARTIALLY_RECEIVED";
            await connection.ExecuteAsync("UPDATE dbo.PurchaseOrders SET Status = @newPoStatus, UpdatedAt = GETDATE() WHERE PurchaseOrderId = @poId;", new { newPoStatus, poId = receipt.PurchaseOrderId }, tx);

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
