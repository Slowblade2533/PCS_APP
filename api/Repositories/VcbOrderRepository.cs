using Dapper;
using Microsoft.Data.SqlClient;
using PCS_API.DTOs;

namespace PCS_API.Repositories;

public class VcbOrderRepository(ISqlConnectionFactory connectionFactory) : IVcbOrderRepository
{
    public async Task<VcbOrderDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        using var conn = connectionFactory.CreateConnection();
        string sql = @"
            SELECT o.Id, o.OrderNo, o.OrderDate, o.TotalAmount, o.Status, o.BranchId,
                   o.Notes, o.TransferSlipUrl, o.CreatedBy, o.CreatedAt, o.UpdatedAt,
                   o.UpdatedBy,
                   uc.Username AS CreatedByUsername,
                   uu.Username AS UpdatedByUsername
            FROM dbo.VcbOrders o
            LEFT JOIN dbo.Users uc ON o.CreatedBy = uc.Id
            LEFT JOIN dbo.Users uu ON o.UpdatedBy = uu.Id
            WHERE o.Id = @Id;

            SELECT i.Id, i.OrderId, i.VariantId, i.Quantity, i.TotalPrice,
                   v.Sku, v.ImageUrl, p.ProductNameTh AS ProductName,
                   COALESCE(NULLIF(v.VariantNameTh, ''), NULLIF(LTRIM(RTRIM(CONCAT(v.Color, ' ', v.SizeLabel, ' ', v.StylePattern))), ''), '') AS VariantName,
                   ISNULL(r.Received, 0) AS ReceivedQuantity,
                   (i.Quantity - ISNULL(r.Received, 0)) AS RemainingQuantity
            FROM dbo.VcbOrderItems i
            INNER JOIN dbo.ProductVariants v ON i.VariantId = v.VariantId
            INNER JOIN dbo.Products p ON v.ProductId = p.ProductId
            LEFT JOIN (
                SELECT si.OrderItemId, SUM(si.GoodQuantity + si.DefectiveQuantity) AS Received
                FROM dbo.VcbShipmentItems si
                INNER JOIN dbo.VcbShipments s ON si.ShipmentId = s.Id
                WHERE s.Status = 'Completed'
                GROUP BY si.OrderItemId
            ) r ON i.Id = r.OrderItemId
            WHERE i.OrderId = @Id;";

        using var multi = await conn.QueryMultipleAsync(new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));
        var order = await multi.ReadFirstOrDefaultAsync<VcbOrderDto>();
        if (order == null) return null;

        order.Items = (await multi.ReadAsync<VcbOrderItemDto>()).ToList();
        return order;
    }

    public async Task<PagedResultDto<VcbOrderDto>> GetPagedAsync(VcbOrderSearchDto search, CancellationToken cancellationToken = default)
    {
        using var conn = connectionFactory.CreateConnection();
        var parameters = new DynamicParameters();
        string whereClause = "WHERE 1=1";

        if (!string.IsNullOrEmpty(search.SearchTerm))
        {
            whereClause += " AND (o.OrderNo LIKE @SearchTerm OR CONVERT(VARCHAR, o.OrderDate, 120) LIKE @SearchTerm)";
            parameters.Add("SearchTerm", $"%{search.SearchTerm}%");
        }

        if (!string.IsNullOrEmpty(search.Status))
        {
            if (search.Status == "Active")
            {
                whereClause += " AND o.Status IN ('Pending', 'Processing')";
            }
            else
            {
                whereClause += " AND o.Status = @Status";
                parameters.Add("Status", search.Status);
            }
        }

        if (search.BranchId.HasValue)
        {
            whereClause += " AND o.BranchId = @BranchId";
            parameters.Add("BranchId", search.BranchId.Value);
        }

        string sql = $@"
            SELECT COUNT(*) FROM dbo.VcbOrders o {whereClause};

            SELECT o.*, 
                   (SELECT COUNT(*) FROM dbo.VcbOrderItems i WHERE i.OrderId = o.Id) AS ItemCount,
                   uc.Username AS CreatedByUsername,
                   uu.Username AS UpdatedByUsername
            FROM dbo.VcbOrders o
            LEFT JOIN dbo.Users uc ON o.CreatedBy = uc.Id
            LEFT JOIN dbo.Users uu ON o.UpdatedBy = uu.Id
            {whereClause}
            ORDER BY o.OrderDate DESC, o.Id DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

        parameters.Add("Offset", search.GetSafeOffset());
        parameters.Add("PageSize", search.PageSize);

        var command = new CommandDefinition(sql, parameters, cancellationToken: cancellationToken);
        using var multi = await conn.QueryMultipleAsync(command);
        int totalCount = await multi.ReadSingleAsync<int>();
        var items = await multi.ReadAsync<VcbOrderDto>();

        return new PagedResultDto<VcbOrderDto>
        {
            Items = items.ToList(),
            TotalCount = totalCount,
            PageNumber = search.PageNumber,
            PageSize = search.PageSize
        };
    }

    public async Task<int> CreateAsync(VcbOrderCreateDto dto, string? transferSlipUrl, int createdBy, CancellationToken cancellationToken = default)
    {
        using var conn = connectionFactory.CreateConnection() as System.Data.Common.DbConnection;
        if (conn == null) throw new InvalidOperationException("Could not create DbConnection.");
        await conn.OpenAsync(cancellationToken);
        using var tx = await conn.BeginTransactionAsync(cancellationToken);

        try
        {
            string insertOrderSql = @"
                INSERT INTO dbo.VcbOrders (OrderNo, OrderDate, TotalAmount, Status, BranchId, Notes, TransferSlipUrl, CreatedBy, CreatedAt, UpdatedAt)
                VALUES (@OrderNo, @OrderDate, @TotalAmount, 'Pending', @BranchId, @Notes, @TransferSlipUrl, @CreatedBy, GETDATE(), GETDATE());
                SELECT CAST(SCOPE_IDENTITY() as int);";
            
            int orderId = await conn.QuerySingleAsync<int>(new CommandDefinition(insertOrderSql, new
            {
                OrderNo = dto.OrderNo,
                OrderDate = dto.OrderDate,
                TotalAmount = dto.TotalAmount,
                BranchId = dto.BranchId,
                Notes = dto.Notes,
                TransferSlipUrl = transferSlipUrl,
                CreatedBy = createdBy
            }, transaction: tx, cancellationToken: cancellationToken));

            if (dto.Items.Any())
            {
                const string insertItemSql = @"
                    INSERT INTO dbo.VcbOrderItems (OrderId, VariantId, Quantity, TotalPrice)
                    VALUES (@OrderId, @VariantId, @Quantity, @TotalPrice);";

                var itemParams = dto.Items.Select(item => new
                {
                    OrderId = orderId,
                    item.VariantId,
                    item.Quantity,
                    item.TotalPrice
                });

                await conn.ExecuteAsync(new CommandDefinition(insertItemSql, itemParams, transaction: tx, cancellationToken: cancellationToken));
            }

            // Sync Financial Transaction
            const string syncFinTxSql = @"
                DECLARE @FinTxId INT;
                SELECT @FinTxId = TransactionId FROM dbo.FinancialTransactions WHERE ReferenceType = 'VcbOrder' AND ReferenceId = @OrderId;

                IF @FinTxId IS NULL
                BEGIN
                    INSERT INTO dbo.FinancialTransactions (
                        TransactionDate, TransactionType, ReferenceType, ReferenceId, TaxInvoiceId,
                        Description, TotalAmount, PaymentMethod, SourceAccountInfo, PaymentRefNo,
                        ReceiverAccountId, AttachmentUrl, ReceivedBy, CreatedBy, CreatedAt, UpdatedAt,
                        Status, BranchId, PostedAt, PostedBy, DocumentNo, PartnerName,
                        SlipDateTime
                    )
                    VALUES (
                        @OrderDate, 'PURCHASE_VCB', 'VcbOrder', @OrderId, NULL,
                        N'ชำระเงินใบสั่งซื้อ VCANBUY เลขที่ ' + @OrderNo, @TotalAmount, 'TRANSFER', NULL, NULL,
                        NULL, @TransferSlipUrl, NULL, @CreatedBy, GETDATE(), GETDATE(),
                        'POSTED', @BranchId, GETDATE(), @CreatedBy, @OrderNo, N'VCANBUY',
                        @OrderDate
                    );
                    SET @FinTxId = SCOPE_IDENTITY();
                END
                ELSE
                BEGIN
                    UPDATE dbo.FinancialTransactions SET
                        TransactionDate = @OrderDate,
                        Description = N'ชำระเงินใบสั่งซื้อ VCANBUY เลขที่ ' + @OrderNo,
                        TotalAmount = @TotalAmount,
                        AttachmentUrl = @TransferSlipUrl,
                        BranchId = @BranchId,
                        DocumentNo = @OrderNo,
                        UpdatedAt = GETDATE()
                    WHERE TransactionId = @FinTxId;
                END

                DELETE FROM dbo.FinancialLedgerEntries WHERE TransactionId = @FinTxId;

                INSERT INTO dbo.FinancialLedgerEntries (TransactionId, AccountId, DebitAmount, CreditAmount, Memo)
                VALUES 
                    (@FinTxId, 4, @TotalAmount, 0, N'ชำระเงินใบสั่งซื้อ VCANBUY เลขที่ ' + @OrderNo),
                    (@FinTxId, 2, 0, @TotalAmount, N'ชำระเงินใบสั่งซื้อ VCANBUY เลขที่ ' + @OrderNo);";

            await conn.ExecuteAsync(new CommandDefinition(syncFinTxSql, new
            {
                OrderId = orderId,
                OrderNo = dto.OrderNo,
                OrderDate = dto.OrderDate,
                TotalAmount = dto.TotalAmount,
                BranchId = dto.BranchId,
                TransferSlipUrl = transferSlipUrl,
                CreatedBy = createdBy
            }, transaction: tx, cancellationToken: cancellationToken));

            await tx.CommitAsync(cancellationToken);
            return orderId;
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<bool> UpdateAsync(int id, VcbOrderCreateDto dto, string? transferSlipUrl, int updatedBy, CancellationToken cancellationToken = default)
    {
        using var conn = connectionFactory.CreateConnection() as System.Data.Common.DbConnection;
        if (conn == null) throw new InvalidOperationException("Could not create DbConnection.");
        await conn.OpenAsync(cancellationToken);
        using var tx = await conn.BeginTransactionAsync(cancellationToken);

        try
        {
            string updateOrderSql = @"
                UPDATE dbo.VcbOrders
                SET OrderNo = @OrderNo,
                    OrderDate = @OrderDate,
                    TotalAmount = @TotalAmount,
                    BranchId = @BranchId,
                    Notes = @Notes,
                    TransferSlipUrl = COALESCE(@TransferSlipUrl, TransferSlipUrl),
                    UpdatedBy = @UpdatedBy,
                    UpdatedAt = GETDATE()
                WHERE Id = @Id AND Status = 'Pending';";

            int rowsAffected = await conn.ExecuteAsync(new CommandDefinition(updateOrderSql, new
            {
                Id = id,
                OrderNo = dto.OrderNo,
                OrderDate = dto.OrderDate,
                TotalAmount = dto.TotalAmount,
                BranchId = dto.BranchId,
                Notes = dto.Notes,
                TransferSlipUrl = transferSlipUrl,
                UpdatedBy = updatedBy
            }, transaction: tx, cancellationToken: cancellationToken));

            if (rowsAffected == 0)
            {
                await tx.RollbackAsync(cancellationToken);
                return false;
            }

            string deleteItemsSql = "DELETE FROM dbo.VcbOrderItems WHERE OrderId = @OrderId;";
            await conn.ExecuteAsync(new CommandDefinition(deleteItemsSql, new { OrderId = id }, transaction: tx, cancellationToken: cancellationToken));

            if (dto.Items.Any())
            {
                const string insertItemSql = @"
                    INSERT INTO dbo.VcbOrderItems (OrderId, VariantId, Quantity, TotalPrice)
                    VALUES (@OrderId, @VariantId, @Quantity, @TotalPrice);";

                var itemParams = dto.Items.Select(item => new
                {
                    OrderId = id,
                    item.VariantId,
                    item.Quantity,
                    item.TotalPrice
                });

                await conn.ExecuteAsync(new CommandDefinition(insertItemSql, itemParams, transaction: tx, cancellationToken: cancellationToken));
            }

            // Sync Financial Transaction
            const string syncFinTxSql = @"
                DECLARE @FinTxId INT;
                SELECT @FinTxId = TransactionId FROM dbo.FinancialTransactions WHERE ReferenceType = 'VcbOrder' AND ReferenceId = @OrderId;

                IF @FinTxId IS NULL
                BEGIN
                    INSERT INTO dbo.FinancialTransactions (
                        TransactionDate, TransactionType, ReferenceType, ReferenceId, TaxInvoiceId,
                        Description, TotalAmount, PaymentMethod, SourceAccountInfo, PaymentRefNo,
                        ReceiverAccountId, AttachmentUrl, ReceivedBy, CreatedBy, CreatedAt, UpdatedAt,
                        Status, BranchId, PostedAt, PostedBy, DocumentNo, PartnerName,
                        SlipDateTime
                    )
                    VALUES (
                        @OrderDate, 'PURCHASE_VCB', 'VcbOrder', @OrderId, NULL,
                        N'ชำระเงินใบสั่งซื้อ VCANBUY เลขที่ ' + @OrderNo, @TotalAmount, 'TRANSFER', NULL, NULL,
                        NULL, @TransferSlipUrl, NULL, @CreatedBy, GETDATE(), GETDATE(),
                        'POSTED', @BranchId, GETDATE(), @CreatedBy, @OrderNo, N'VCANBUY',
                        @OrderDate
                    );
                    SET @FinTxId = SCOPE_IDENTITY();
                END
                ELSE
                BEGIN
                    UPDATE dbo.FinancialTransactions SET
                        TransactionDate = @OrderDate,
                        Description = N'ชำระเงินใบสั่งซื้อ VCANBUY เลขที่ ' + @OrderNo,
                        TotalAmount = @TotalAmount,
                        AttachmentUrl = @TransferSlipUrl,
                        BranchId = @BranchId,
                        DocumentNo = @OrderNo,
                        UpdatedAt = GETDATE()
                    WHERE TransactionId = @FinTxId;
                END

                DELETE FROM dbo.FinancialLedgerEntries WHERE TransactionId = @FinTxId;

                INSERT INTO dbo.FinancialLedgerEntries (TransactionId, AccountId, DebitAmount, CreditAmount, Memo)
                VALUES 
                    (@FinTxId, 4, @TotalAmount, 0, N'ชำระเงินใบสั่งซื้อ VCANBUY เลขที่ ' + @OrderNo),
                    (@FinTxId, 2, 0, @TotalAmount, N'ชำระเงินใบสั่งซื้อ VCANBUY เลขที่ ' + @OrderNo);";

            await conn.ExecuteAsync(new CommandDefinition(syncFinTxSql, new
            {
                OrderId = id,
                OrderNo = dto.OrderNo,
                OrderDate = dto.OrderDate,
                TotalAmount = dto.TotalAmount,
                BranchId = dto.BranchId,
                TransferSlipUrl = transferSlipUrl,
                CreatedBy = updatedBy
            }, transaction: tx, cancellationToken: cancellationToken));

            await tx.CommitAsync(cancellationToken);
            return true;
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<bool> UpdateStatusAsync(int id, string status, int currentUserId, System.Data.IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        var conn = transaction?.Connection ?? connectionFactory.CreateConnection();
        try
        {
            string sql = "UPDATE dbo.VcbOrders SET Status = @Status, UpdatedBy = @UpdatedBy, UpdatedAt = GETDATE() WHERE Id = @Id";
            int rows = await conn.ExecuteAsync(new CommandDefinition(sql, new { Id = id, Status = status, UpdatedBy = currentUserId }, transaction: transaction, cancellationToken: cancellationToken));
            return rows > 0;
        }
        finally
        {
            if (transaction == null) conn.Dispose();
        }
    }
}
