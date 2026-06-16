using Dapper;
using Microsoft.Data.SqlClient;
using PCS_API.DTOs;
using System.Text;
using PCS_API.Models;

namespace PCS_API.Repositories;

public class VcbShipmentRepository : IVcbShipmentRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public VcbShipmentRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<VcbShipmentDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        string sql = @"
            SELECT *, (SELECT DeliveryNo FROM dbo.VcbDeliveries WHERE Id = dbo.VcbShipments.DeliveryId) AS DeliveryNo FROM dbo.VcbShipments WHERE Id = @Id;
            
            SELECT i.*, v.Sku, v.ImageUrl, p.ProductNameTh AS ProductName, 
                   COALESCE(NULLIF(v.VariantNameTh, ''), NULLIF(LTRIM(RTRIM(CONCAT(v.Color, ' ', v.SizeLabel, ' ', v.StylePattern))), ''), '') AS VariantName
            FROM dbo.VcbShipmentItems i
            INNER JOIN dbo.ProductVariants v ON i.VariantId = v.VariantId
            INNER JOIN dbo.Products p ON v.ProductId = p.ProductId
            WHERE i.ShipmentId = @Id;";

        using var multi = await conn.QueryMultipleAsync(new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));
        var shipment = await multi.ReadFirstOrDefaultAsync<VcbShipmentDto>();
        if (shipment == null) return null;

        shipment.Items = (await multi.ReadAsync<VcbShipmentItemDto>()).ToList();
        return shipment;
    }

    public async Task<PagedResultDto<VcbShipmentDto>> GetPagedAsync(VcbShipmentSearchDto search, CancellationToken cancellationToken = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        var parameters = new DynamicParameters();
        string whereClause = "WHERE 1=1";

        if (!string.IsNullOrEmpty(search.SearchTerm))
        {
            whereClause += " AND (ReceiptNo LIKE @SearchTerm)";
            parameters.Add("SearchTerm", $"%{search.SearchTerm}%");
        }

        if (!string.IsNullOrEmpty(search.Status))
        {
            whereClause += " AND Status = @Status";
            parameters.Add("Status", search.Status);
        }

        if (search.DeliveryId.HasValue)
        {
            whereClause += " AND DeliveryId = @DeliveryId";
            parameters.Add("DeliveryId", search.DeliveryId.Value);
        }

        string sql = $@"
            SELECT COUNT(*) FROM dbo.VcbShipments {whereClause};

            SELECT *, 
                   (SELECT COUNT(*) FROM dbo.VcbShipmentItems WHERE ShipmentId = dbo.VcbShipments.Id) AS ItemsCount,
                   (SELECT DeliveryNo FROM dbo.VcbDeliveries WHERE Id = dbo.VcbShipments.DeliveryId) AS DeliveryNo 
            FROM dbo.VcbShipments
            {whereClause}
            ORDER BY ReceiptDate DESC, Id DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

        parameters.Add("Offset", search.GetSafeOffset());
        parameters.Add("PageSize", search.PageSize);

        var command = new CommandDefinition(sql, parameters, cancellationToken: cancellationToken);
        using var multi = await conn.QueryMultipleAsync(command);
        int totalCount = await multi.ReadSingleAsync<int>();
        var items = await multi.ReadAsync<VcbShipmentDto>();

        return new PagedResultDto<VcbShipmentDto>
        {
            Items = items.ToList(),
            TotalCount = totalCount,
            PageNumber = search.PageNumber,
            PageSize = search.PageSize
        };
    }

    public async Task<int> CreateAsync(VcbShipmentCreateDto dto, int createdBy, CancellationToken cancellationToken = default)
    {
        using var conn = _connectionFactory.CreateConnection() as System.Data.Common.DbConnection;
        if (conn == null) throw new InvalidOperationException("Could not create DbConnection.");
        await conn.OpenAsync(cancellationToken);
        using var tx = await conn.BeginTransactionAsync(cancellationToken);

        try
        {
            string insertSql = @"
                INSERT INTO dbo.VcbShipments (ReceiptNo, ReceiptDate, DeliveryId, Status, Notes, IsForceCloseOrder, CreatedBy, CreatedAt, UpdatedAt)
                VALUES ('RECV' + FORMAT(GETDATE(), 'yyMMddHHmmss'), GETDATE(), @DeliveryId, 'Draft', @Notes, @IsForceCloseOrder, @CreatedBy, GETDATE(), GETDATE());
                SELECT CAST(SCOPE_IDENTITY() as int);";
            
            int shipmentId = await conn.QuerySingleAsync<int>(new CommandDefinition(insertSql, new
            {
                DeliveryId = dto.DeliveryId,
                Notes = dto.Notes,
                IsForceCloseOrder = dto.IsForceCloseOrder,
                CreatedBy = createdBy
            }, transaction: tx, cancellationToken: cancellationToken));

            if (dto.Items.Any())
            {
                var batchSql = new StringBuilder();
                var batchParams = new DynamicParameters();
                batchParams.Add("ShipmentId", shipmentId);

                for (int i = 0; i < dto.Items.Count; i++)
                {
                    var item = dto.Items[i];
                    batchSql.AppendLine($@"
                        INSERT INTO dbo.VcbShipmentItems (ShipmentId, OrderItemId, VariantId, BoxNumbers, ReceiptStatus, ExpectedQuantity, GoodQuantity, DefectiveQuantity, RefundAmount)
                        VALUES (@ShipmentId, @OrderItemId{i}, @VariantId{i}, @BoxNumbers{i}, @ReceiptStatus{i}, @ExpectedQuantity{i}, @GoodQuantity{i}, @DefectiveQuantity{i}, @RefundAmount{i});");
                    
                    batchParams.Add($"OrderItemId{i}", item.OrderItemId);
                    batchParams.Add($"VariantId{i}", item.VariantId);
                    batchParams.Add($"BoxNumbers{i}", item.BoxNumbers);
                    batchParams.Add($"ReceiptStatus{i}", item.ReceiptStatus);
                    batchParams.Add($"ExpectedQuantity{i}", item.ExpectedQuantity);
                    batchParams.Add($"GoodQuantity{i}", item.GoodQuantity);
                    batchParams.Add($"DefectiveQuantity{i}", item.DefectiveQuantity);
                    batchParams.Add($"RefundAmount{i}", item.RefundAmount);
                }

                await conn.ExecuteAsync(new CommandDefinition(batchSql.ToString(), batchParams, transaction: tx, cancellationToken: cancellationToken));
            }

            await tx.CommitAsync(cancellationToken);
            return shipmentId;
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<bool> UpdateStatusAsync(int id, string status, int currentUserId, CancellationToken cancellationToken = default)
    {
        using var conn = _connectionFactory.CreateConnection() as System.Data.Common.DbConnection;
        if (conn == null) throw new InvalidOperationException("Could not create DbConnection.");
        await conn.OpenAsync(cancellationToken);
        using var tx = await conn.BeginTransactionAsync(cancellationToken);

        try
        {
            string getShipmentSql = @"
                SELECT s.DeliveryId, s.Status FROM dbo.VcbShipments s WHERE s.Id = @Id;
                SELECT * FROM dbo.VcbShipmentItems WHERE ShipmentId = @Id;
            ";

            using var multi = await conn.QueryMultipleAsync(new CommandDefinition(getShipmentSql, new { Id = id }, transaction: tx, cancellationToken: cancellationToken));
            var shipmentInfo = await multi.ReadFirstOrDefaultAsync<(int DeliveryId, string Status)>();
            if (shipmentInfo == default || shipmentInfo.Status != "Draft")
            {
                // Can only update status from Draft
                return false;
            }

            var items = (await multi.ReadAsync<VcbShipmentItemModel>()).ToList();

            string updateSql = @"
                UPDATE dbo.VcbShipments
                SET Status = @Status,
                    UpdatedAt = GETDATE()
                WHERE Id = @Id;";
            await conn.ExecuteAsync(new CommandDefinition(updateSql, new { Id = id, Status = status }, transaction: tx, cancellationToken: cancellationToken));

            if (status == "Completed")
            {
                bool isForceClose = await conn.QuerySingleOrDefaultAsync<bool>(new CommandDefinition(
                    "SELECT IsForceCloseOrder FROM dbo.VcbShipments WHERE Id = @Id;",
                    new { Id = id }, transaction: tx, cancellationToken: cancellationToken));

                // Check if all expected CX boxes have been received
                string getDeliveryItemsSql = "SELECT ContainedBoxNumbers FROM dbo.VcbDeliveryItems WHERE DeliveryId = @DeliveryId;";
                var deliveryItems = (await conn.QueryAsync<string>(new CommandDefinition(getDeliveryItemsSql, new { DeliveryId = shipmentInfo.DeliveryId }, transaction: tx, cancellationToken: cancellationToken))).ToList();

                var expectedCxBoxes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var json in deliveryItems)
                {
                    if (string.IsNullOrEmpty(json)) continue;
                    try
                    {
                        using var doc = System.Text.Json.JsonDocument.Parse(json);
                        if (doc.RootElement.ValueKind == System.Text.Json.JsonValueKind.Array)
                        {
                            foreach (var element in doc.RootElement.EnumerateArray())
                            {
                                if (element.TryGetProperty("boxNo", out var boxNoProp))
                                {
                                    var boxNo = boxNoProp.GetString();
                                    if (!string.IsNullOrEmpty(boxNo))
                                    {
                                        expectedCxBoxes.Add(boxNo.Trim());
                                    }
                                }
                            }
                        }
                    }
                    catch
                    {
                        expectedCxBoxes.Add(json.Trim());
                    }
                }

                string getReceivedBoxesSql = @"
                    SELECT si.BoxNumbers 
                    FROM dbo.VcbShipmentItems si
                    INNER JOIN dbo.VcbShipments s ON si.ShipmentId = s.Id
                    WHERE s.DeliveryId = @DeliveryId AND (s.Status = 'Completed' OR s.Id = @CurrentShipmentId);";
                
                var receivedBoxesRaw = (await conn.QueryAsync<string>(new CommandDefinition(getReceivedBoxesSql, new { DeliveryId = shipmentInfo.DeliveryId, CurrentShipmentId = id }, transaction: tx, cancellationToken: cancellationToken))).ToList();

                var receivedCxBoxes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var boxStr in receivedBoxesRaw)
                {
                    if (string.IsNullOrEmpty(boxStr)) continue;
                    var parts = boxStr.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var part in parts)
                    {
                        receivedCxBoxes.Add(part.Trim());
                    }
                }

                bool allReceived = true;
                if (expectedCxBoxes.Count > 0)
                {
                    foreach (var expected in expectedCxBoxes)
                    {
                        if (!receivedCxBoxes.Contains(expected))
                        {
                            allReceived = false;
                            break;
                        }
                    }
                }

                if (isForceClose || allReceived)
                {
                    string updateDeliverySql = "UPDATE dbo.VcbDeliveries SET Status = 'Completed', UpdatedAt = GETDATE() WHERE Id = @DeliveryId;";
                    await conn.ExecuteAsync(new CommandDefinition(updateDeliverySql, new { DeliveryId = shipmentInfo.DeliveryId }, transaction: tx, cancellationToken: cancellationToken));
                }

                string getOrderIdsSql = @"SELECT DISTINCT oi.OrderId FROM dbo.VcbShipmentItems si INNER JOIN dbo.VcbOrderItems oi ON si.OrderItemId = oi.Id WHERE si.ShipmentId = @Id";
                var orderIds = (await conn.QueryAsync<int>(new CommandDefinition(getOrderIdsSql, new { Id = id }, transaction: tx, cancellationToken: cancellationToken))).ToList();

                foreach (var orderId in orderIds)
                {
                    bool fullyFulfilled = false;
                    if (!isForceClose)
                    {
                        string checkFulfilledSql = @"
                            SELECT CASE 
                                WHEN EXISTS (
                                    SELECT 1 
                                    FROM dbo.VcbOrderItems oi
                                    LEFT JOIN (
                                        SELECT si.OrderItemId, SUM(si.GoodQuantity + si.DefectiveQuantity) as Received
                                        FROM dbo.VcbShipmentItems si
                                        INNER JOIN dbo.VcbShipments s ON si.ShipmentId = s.Id
                                        WHERE s.Status = 'Completed' OR s.Id = @CurrentShipmentId
                                        GROUP BY si.OrderItemId
                                    ) r ON oi.Id = r.OrderItemId
                                    WHERE oi.OrderId = @OrderId
                                      AND oi.Quantity > ISNULL(r.Received, 0)
                                ) THEN 0 
                                ELSE 1 
                            END;";
                        fullyFulfilled = await conn.QuerySingleAsync<bool>(new CommandDefinition(checkFulfilledSql, new { OrderId = orderId, CurrentShipmentId = id }, transaction: tx, cancellationToken: cancellationToken));
                    }

                    if (isForceClose || fullyFulfilled)
                    {
                        string updateOrderSql = @"UPDATE dbo.VcbOrders SET Status = 'Completed', UpdatedAt = GETDATE() WHERE Id = @OrderId AND Status != 'Completed';";
                        await conn.ExecuteAsync(new CommandDefinition(updateOrderSql, new { OrderId = orderId }, transaction: tx, cancellationToken: cancellationToken));
                    }
                }

                // Process Stocks and Refunds
                var stockSql = new StringBuilder();
                var stockParams = new DynamicParameters();
                stockParams.Add("CreatedBy", currentUserId);
                stockParams.Add("ShipmentId", id);

                int branchId = 1;
                if (orderIds.Any())
                {
                    string getBranchSql = "SELECT TOP 1 BranchId FROM dbo.VcbOrders WHERE Id = @OrderId";
                    branchId = await conn.QuerySingleAsync<int>(new CommandDefinition(getBranchSql, new { OrderId = orderIds.First() }, transaction: tx, cancellationToken: cancellationToken));
                }
                stockParams.Add("BranchId", branchId);

                for (int i = 0; i < items.Count; i++)
                {
                    var item = items[i];

                    if (item.GoodQuantity > 0)
                    {
                        // Add to Stocks
                        stockSql.AppendLine($@"
                            UPDATE dbo.Stocks
                            SET CurrentQuantity = CurrentQuantity + @GoodQty{i},
                                UpdatedAt = GETDATE()
                            WHERE VariantId = @VarId{i};
                            
                            IF @@ROWCOUNT = 0
                            BEGIN
                                INSERT INTO dbo.Stocks (VariantId, CurrentQuantity, ReservedQuantity, ReorderPoint, UpdatedAt)
                                VALUES (@VarId{i}, @GoodQty{i}, 0, 0, GETDATE());
                            END

                            INSERT INTO dbo.StockTransactions (VariantId, BranchId, TransactionType, Quantity, UnitCost, Notes, CreatedBy, CreatedAt)
                            VALUES (@VarId{i}, @BranchId, 'IN', @GoodQty{i}, 0.00, N'รับสินค้าจาก VCANBUY Shipment #' + CAST(@ShipmentId AS VARCHAR), @CreatedBy, GETDATE());
                        ");
                        stockParams.Add($"GoodQty{i}", item.GoodQuantity);
                        stockParams.Add($"VarId{i}", item.VariantId);
                    }

                    if (item.RefundAmount > 0)
                    {
                        stockSql.AppendLine($@"
                            INSERT INTO dbo.AccountTransactions (TransactionDate, Type, Amount, ReferenceType, ReferenceId, Notes, CreatedBy, CreatedAt)
                            VALUES (GETDATE(), 'Income', @Refund{i}, 'VcbShipmentRefund', @ShipmentId, N'คืนเงินจาก VCANBUY รายการที่ ' + CAST(@VarId{i} AS VARCHAR), @CreatedBy, GETDATE());
                        ");
                        stockParams.Add($"Refund{i}", item.RefundAmount);
                    }
                }

                if (stockSql.Length > 0)
                {
                    await conn.ExecuteAsync(new CommandDefinition(stockSql.ToString(), stockParams, transaction: tx, cancellationToken: cancellationToken));
                }
            }

            await tx.CommitAsync(cancellationToken);
            return true;
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<bool> UpdateAsync(int id, VcbShipmentCreateDto dto, CancellationToken cancellationToken = default)
    {
        using var conn = _connectionFactory.CreateConnection() as System.Data.Common.DbConnection;
        if (conn == null) throw new InvalidOperationException("Could not create DbConnection.");
        await conn.OpenAsync(cancellationToken);
        using var tx = await conn.BeginTransactionAsync(cancellationToken);

        try
        {
            // Verify existence and Draft status
            string checkSql = "SELECT Status FROM dbo.VcbShipments WHERE Id = @Id;";
            var status = await conn.QuerySingleOrDefaultAsync<string>(new CommandDefinition(checkSql, new { Id = id }, transaction: tx, cancellationToken: cancellationToken));
            if (status == null || status != "Draft")
            {
                return false;
            }

            // Update parent
            string updateParentSql = @"
                UPDATE dbo.VcbShipments 
                SET DeliveryId = @DeliveryId,
                    Notes = @Notes,
                    IsForceCloseOrder = @IsForceCloseOrder,
                    UpdatedAt = GETDATE()
                WHERE Id = @Id;";
            await conn.ExecuteAsync(new CommandDefinition(updateParentSql, new
            {
                Id = id,
                DeliveryId = dto.DeliveryId,
                Notes = dto.Notes,
                IsForceCloseOrder = dto.IsForceCloseOrder
            }, transaction: tx, cancellationToken: cancellationToken));

            // Delete old items
            string deleteItemsSql = "DELETE FROM dbo.VcbShipmentItems WHERE ShipmentId = @Id;";
            await conn.ExecuteAsync(new CommandDefinition(deleteItemsSql, new { Id = id }, transaction: tx, cancellationToken: cancellationToken));

            // Insert new items
            if (dto.Items.Any())
            {
                var batchSql = new StringBuilder();
                var batchParams = new DynamicParameters();
                batchParams.Add("ShipmentId", id);

                for (int i = 0; i < dto.Items.Count; i++)
                {
                    var item = dto.Items[i];
                    batchSql.AppendLine($@"
                        INSERT INTO dbo.VcbShipmentItems (ShipmentId, OrderItemId, VariantId, BoxNumbers, ReceiptStatus, ExpectedQuantity, GoodQuantity, DefectiveQuantity, RefundAmount)
                        VALUES (@ShipmentId, @OrderItemId{i}, @VariantId{i}, @BoxNumbers{i}, @ReceiptStatus{i}, @ExpectedQuantity{i}, @GoodQuantity{i}, @DefectiveQuantity{i}, @RefundAmount{i});");
                    
                    batchParams.Add($"OrderItemId{i}", item.OrderItemId);
                    batchParams.Add($"VariantId{i}", item.VariantId);
                    batchParams.Add($"BoxNumbers{i}", item.BoxNumbers);
                    batchParams.Add($"ReceiptStatus{i}", item.ReceiptStatus);
                    batchParams.Add($"ExpectedQuantity{i}", item.ExpectedQuantity);
                    batchParams.Add($"GoodQuantity{i}", item.GoodQuantity);
                    batchParams.Add($"DefectiveQuantity{i}", item.DefectiveQuantity);
                    batchParams.Add($"RefundAmount{i}", item.RefundAmount);
                }

                await conn.ExecuteAsync(new CommandDefinition(batchSql.ToString(), batchParams, transaction: tx, cancellationToken: cancellationToken));
            }

            await tx.CommitAsync(cancellationToken);
            return true;
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
