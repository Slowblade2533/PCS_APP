using Dapper;
using Microsoft.Data.SqlClient;
using PCS_API.DTOs;
using System.Text;
using PCS_API.Models;

namespace PCS_API.Repositories;

public class VcbShipmentRepository(ISqlConnectionFactory connectionFactory) : IVcbShipmentRepository
{
    public async Task<VcbShipmentDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        using var conn = connectionFactory.CreateConnection();
        string sql = @"
            SELECT Id, ReceiptNo, ReceiptDate, DeliveryId, Status, Notes, IsForceCloseOrder, CreatedBy, UpdatedBy, CreatedAt, UpdatedAt, (SELECT DeliveryNo FROM dbo.VcbDeliveries WHERE Id = dbo.VcbShipments.DeliveryId) AS DeliveryNo FROM dbo.VcbShipments WHERE Id = @Id;
            
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
        using var conn = connectionFactory.CreateConnection();
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

            SELECT Id, ReceiptNo, ReceiptDate, DeliveryId, Status, Notes, IsForceCloseOrder, CreatedBy, UpdatedBy, CreatedAt, UpdatedAt, 
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

    public async Task<int> CreateAsync(VcbShipmentCreateDto dto, int createdBy, System.Data.IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        var conn = transaction?.Connection ?? connectionFactory.CreateConnection();
        try
        {
            string insertSql = @"
                INSERT INTO dbo.VcbShipments (ReceiptNo, ReceiptDate, DeliveryId, Status, Notes, IsForceCloseOrder, CreatedBy, CreatedAt, UpdatedAt)
                VALUES ('RECV' + FORMAT(GETDATE(), 'yyMMddHHmmss'), @ReceiptDate, @DeliveryId, 'Draft', @Notes, @IsForceCloseOrder, @CreatedBy, GETDATE(), GETDATE());
                SELECT CAST(SCOPE_IDENTITY() as int);";
            
            int shipmentId = await conn.QuerySingleAsync<int>(new CommandDefinition(insertSql, new
            {
                ReceiptDate = dto.ReceiptDate ?? DateTime.UtcNow,
                DeliveryId = dto.DeliveryId,
                Notes = dto.Notes,
                IsForceCloseOrder = dto.IsForceCloseOrder,
                CreatedBy = createdBy
            }, transaction: transaction, cancellationToken: cancellationToken));

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

                await conn.ExecuteAsync(new CommandDefinition(batchSql.ToString(), batchParams, transaction: transaction, cancellationToken: cancellationToken));
            }

            return shipmentId;
        }
        finally
        {
            if (transaction == null) conn.Dispose();
        }
    }

    public async Task<bool> UpdateStatusAsync(int id, string status, int currentUserId, System.Data.IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        var conn = transaction?.Connection ?? connectionFactory.CreateConnection();
        try
        {
            string sql = "UPDATE dbo.VcbShipments SET Status = @Status, UpdatedBy = @UpdatedBy, UpdatedAt = GETDATE() WHERE Id = @Id";
            int rows = await conn.ExecuteAsync(new CommandDefinition(sql, new { Id = id, Status = status, UpdatedBy = currentUserId }, transaction: transaction, cancellationToken: cancellationToken));
            return rows > 0;
        }
        finally
        {
            if (transaction == null) conn.Dispose();
        }
    }

    public async Task<bool> UpdateAsync(int id, VcbShipmentCreateDto dto, int currentUserId, bool isSuperuser, System.Data.IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        var conn = transaction?.Connection ?? connectionFactory.CreateConnection();
        try
        {
            string updateParentSql = @"
                UPDATE dbo.VcbShipments 
                SET DeliveryId = @DeliveryId,
                    ReceiptDate = @ReceiptDate,
                    Notes = @Notes,
                    IsForceCloseOrder = @IsForceCloseOrder,
                    UpdatedAt = GETDATE(),
                    UpdatedBy = @UpdatedBy
                WHERE Id = @Id AND (Status = 'Draft' OR @IsSuperuser = 1);";
            
            int rows = await conn.ExecuteAsync(new CommandDefinition(updateParentSql, new
            {
                Id = id,
                DeliveryId = dto.DeliveryId,
                ReceiptDate = dto.ReceiptDate ?? DateTime.UtcNow,
                Notes = dto.Notes,
                IsForceCloseOrder = dto.IsForceCloseOrder,
                UpdatedBy = currentUserId,
                IsSuperuser = isSuperuser ? 1 : 0
            }, transaction: transaction, cancellationToken: cancellationToken));

            if (rows == 0) return false;

            string deleteItemsSql = "DELETE FROM dbo.VcbShipmentItems WHERE ShipmentId = @Id;";
            await conn.ExecuteAsync(new CommandDefinition(deleteItemsSql, new { Id = id }, transaction: transaction, cancellationToken: cancellationToken));

            if (dto.Items != null && dto.Items.Any())
            {
                var batchSql = new System.Text.StringBuilder();
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

                await conn.ExecuteAsync(new CommandDefinition(batchSql.ToString(), batchParams, transaction: transaction, cancellationToken: cancellationToken));
            }

            return true;
        }
        finally
        {
            if (transaction == null) conn.Dispose();
        }
    }

    public async Task<(int DeliveryId, string Status, bool IsForceCloseOrder)> GetShipmentInfoAsync(int id, System.Data.IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        var conn = transaction?.Connection ?? connectionFactory.CreateConnection();
        try
        {
            string sql = "SELECT DeliveryId, Status, IsForceCloseOrder FROM dbo.VcbShipments WHERE Id = @Id;";
            return await conn.QuerySingleOrDefaultAsync<(int, string, bool)>(new CommandDefinition(sql, new { Id = id }, transaction: transaction, cancellationToken: cancellationToken));
        }
        finally
        {
            if (transaction == null) conn.Dispose();
        }
    }

    public async Task<IEnumerable<VcbShipmentItemModel>> GetShipmentItemsAsync(int id, System.Data.IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        var conn = transaction?.Connection ?? connectionFactory.CreateConnection();
        try
        {
            string sql = "SELECT Id, ShipmentId, OrderItemId, VariantId, BoxNumbers, ReceiptStatus, ExpectedQuantity, GoodQuantity, DefectiveQuantity, RefundAmount FROM dbo.VcbShipmentItems WHERE ShipmentId = @Id;";
            return await conn.QueryAsync<VcbShipmentItemModel>(new CommandDefinition(sql, new { Id = id }, transaction: transaction, cancellationToken: cancellationToken));
        }
        finally
        {
            if (transaction == null) conn.Dispose();
        }
    }

    public async Task<IEnumerable<string>> GetReceivedShipmentBoxesAsync(int deliveryId, int? excludeShipmentId = null, System.Data.IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        var conn = transaction?.Connection ?? connectionFactory.CreateConnection();
        try
        {
            string sql = @"
                SELECT si.BoxNumbers 
                FROM dbo.VcbShipmentItems si
                INNER JOIN dbo.VcbShipments s ON si.ShipmentId = s.Id
                WHERE s.DeliveryId = @DeliveryId AND s.Status = 'Completed'";
            
            if (excludeShipmentId.HasValue)
            {
                sql += " AND s.Id != @ExcludeId;";
            }
            
            return await conn.QueryAsync<string>(new CommandDefinition(sql, new { DeliveryId = deliveryId, ExcludeId = excludeShipmentId }, transaction: transaction, cancellationToken: cancellationToken));
        }
        finally
        {
            if (transaction == null) conn.Dispose();
        }
    }

    public async Task<List<int>> GetOrderIdsByShipmentIdAsync(int shipmentId, System.Data.IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        var conn = transaction?.Connection ?? connectionFactory.CreateConnection();
        try
        {
            string sql = @"SELECT DISTINCT oi.OrderId FROM dbo.VcbShipmentItems si INNER JOIN dbo.VcbOrderItems oi ON si.OrderItemId = oi.Id WHERE si.ShipmentId = @Id";
            var ids = await conn.QueryAsync<int>(new CommandDefinition(sql, new { Id = shipmentId }, transaction: transaction, cancellationToken: cancellationToken));
            return ids.ToList();
        }
        finally
        {
            if (transaction == null) conn.Dispose();
        }
    }

    public async Task<List<int>> GetFulfilledOrderIdsAsync(int shipmentId, System.Data.IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        var conn = transaction?.Connection ?? connectionFactory.CreateConnection();
        try
        {
            string sql = @"
                SELECT DISTINCT oi.OrderId
                FROM dbo.VcbShipmentItems si
                INNER JOIN dbo.VcbOrderItems oi ON si.OrderItemId = oi.Id
                WHERE si.ShipmentId = @CurrentShipmentId
                  AND oi.OrderId NOT IN (
                      SELECT DISTINCT oi2.OrderId
                      FROM dbo.VcbOrderItems oi2
                      LEFT JOIN (
                          SELECT si2.OrderItemId, SUM(si2.GoodQuantity + si2.DefectiveQuantity) as Received
                          FROM dbo.VcbShipmentItems si2
                          INNER JOIN dbo.VcbShipments s2 ON si2.ShipmentId = s2.Id
                          WHERE s2.Status = 'Completed' OR s2.Id = @CurrentShipmentId
                          GROUP BY si2.OrderItemId
                      ) r2 ON oi2.Id = r2.OrderItemId
                      WHERE oi2.Quantity > ISNULL(r2.Received, 0)
                  );";
            var ids = await conn.QueryAsync<int>(new CommandDefinition(sql, new { CurrentShipmentId = shipmentId }, transaction: transaction, cancellationToken: cancellationToken));
            return ids.ToList();
        }
        finally
        {
            if (transaction == null) conn.Dispose();
        }
    }

    public async Task<int> GetBranchIdByOrderIdAsync(int orderId, System.Data.IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        var conn = transaction?.Connection ?? connectionFactory.CreateConnection();
        try
        {
            string sql = "SELECT TOP 1 BranchId FROM dbo.VcbOrders WHERE Id = @OrderId";
            return await conn.QuerySingleOrDefaultAsync<int>(new CommandDefinition(sql, new { OrderId = orderId }, transaction: transaction, cancellationToken: cancellationToken));
        }
        finally
        {
            if (transaction == null) conn.Dispose();
        }
    }
}