using System;
using System.IO;

class Program
{
    static void Main()
    {
        string path = @"d:\PCS_APP\api\Repositories\VcbShipmentRepository.cs";
        string content = File.ReadAllText(path);
        
        int index = content.IndexOf("public async Task<bool> UpdateStatusAsync");
        if (index >= 0)
        {
            content = content.Substring(0, index);
        }

        string newMethods = @"
    public async Task<bool> UpdateStatusAsync(int id, string status, int currentUserId, System.Data.IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        var conn = transaction?.Connection ?? _connectionFactory.CreateConnection();
        try
        {
            string sql = ""UPDATE dbo.VcbShipments SET Status = @Status, UpdatedBy = @UpdatedBy, UpdatedAt = GETDATE() WHERE Id = @Id"";
            int rows = await conn.QuerySingleOrDefaultAsync<int>(sql, new { Id = id, Status = status, UpdatedBy = currentUserId }, transaction: transaction);
            return true;
        }
        finally
        {
            if (transaction == null) conn.Dispose();
        }
    }

    public async Task<bool> UpdateAsync(int id, PCS_API.DTOs.VcbShipmentCreateDto dto, int currentUserId, bool isSuperuser, System.Data.IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        var conn = transaction?.Connection ?? _connectionFactory.CreateConnection();
        try
        {
            string updateParentSql = @""
                UPDATE dbo.VcbShipments 
                SET DeliveryId = @DeliveryId,
                    Notes = @Notes,
                    IsForceCloseOrder = @IsForceCloseOrder,
                    UpdatedAt = GETDATE(),
                    UpdatedBy = @UpdatedBy
                WHERE Id = @Id AND (Status = 'Draft' OR @IsSuperuser = 1);"";
            
            int rows = await conn.ExecuteAsync(updateParentSql, new
            {
                Id = id,
                DeliveryId = dto.DeliveryId,
                Notes = dto.Notes,
                IsForceCloseOrder = dto.IsForceCloseOrder,
                UpdatedBy = currentUserId,
                IsSuperuser = isSuperuser ? 1 : 0
            }, transaction: transaction);

            if (rows == 0) return false;

            string deleteItemsSql = ""DELETE FROM dbo.VcbShipmentItems WHERE ShipmentId = @Id;"";
            await conn.ExecuteAsync(deleteItemsSql, new { Id = id }, transaction: transaction);

            if (dto.Items != null && dto.Items.Count > 0)
            {
                var batchSql = new System.Text.StringBuilder();
                var batchParams = new Dapper.DynamicParameters();
                batchParams.Add(""ShipmentId"", id);

                for (int i = 0; i < dto.Items.Count; i++)
                {
                    var item = dto.Items[i];
                    batchSql.AppendLine($@""
                        INSERT INTO dbo.VcbShipmentItems (ShipmentId, OrderItemId, VariantId, BoxNumbers, ReceiptStatus, ExpectedQuantity, GoodQuantity, DefectiveQuantity, RefundAmount)
                        VALUES (@ShipmentId, @OrderItemId{i}, @VariantId{i}, @BoxNumbers{i}, @ReceiptStatus{i}, @ExpectedQuantity{i}, @GoodQuantity{i}, @DefectiveQuantity{i}, @RefundAmount{i});"");
                    
                    batchParams.Add($""OrderItemId{i}"", item.OrderItemId);
                    batchParams.Add($""VariantId{i}"", item.VariantId);
                    batchParams.Add($""BoxNumbers{i}"", item.BoxNumbers);
                    batchParams.Add($""ReceiptStatus{i}"", item.ReceiptStatus);
                    batchParams.Add($""ExpectedQuantity{i}"", item.ExpectedQuantity);
                    batchParams.Add($""GoodQuantity{i}"", item.GoodQuantity);
                    batchParams.Add($""DefectiveQuantity{i}"", item.DefectiveQuantity);
                    batchParams.Add($""RefundAmount{i}"", item.RefundAmount);
                }

                await conn.ExecuteAsync(batchSql.ToString(), batchParams, transaction: transaction);
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
        var conn = transaction?.Connection ?? _connectionFactory.CreateConnection();
        try
        {
            string sql = ""SELECT DeliveryId, Status, IsForceCloseOrder FROM dbo.VcbShipments WHERE Id = @Id;"";
            return await conn.QuerySingleOrDefaultAsync<(int, string, bool)>(sql, new { Id = id }, transaction: transaction);
        }
        finally
        {
            if (transaction == null) conn.Dispose();
        }
    }

    public async Task<System.Collections.Generic.IEnumerable<PCS_API.Models.VcbShipmentItemModel>> GetShipmentItemsAsync(int id, System.Data.IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        var conn = transaction?.Connection ?? _connectionFactory.CreateConnection();
        try
        {
            string sql = ""SELECT * FROM dbo.VcbShipmentItems WHERE ShipmentId = @Id;"";
            return await conn.QueryAsync<PCS_API.Models.VcbShipmentItemModel>(sql, new { Id = id }, transaction: transaction);
        }
        finally
        {
            if (transaction == null) conn.Dispose();
        }
    }

    public async Task<System.Collections.Generic.IEnumerable<string>> GetReceivedShipmentBoxesAsync(int deliveryId, int? excludeShipmentId = null, System.Data.IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        var conn = transaction?.Connection ?? _connectionFactory.CreateConnection();
        try
        {
            string sql = @""
                SELECT si.BoxNumbers 
                FROM dbo.VcbShipmentItems si
                INNER JOIN dbo.VcbShipments s ON si.ShipmentId = s.Id
                WHERE s.DeliveryId = @DeliveryId AND s.Status = 'Completed'"";
            
            if (excludeShipmentId.HasValue)
            {
                sql += "" AND s.Id != @ExcludeId;"";
            }
            
            return await conn.QueryAsync<string>(sql, new { DeliveryId = deliveryId, ExcludeId = excludeShipmentId }, transaction: transaction);
        }
        finally
        {
            if (transaction == null) conn.Dispose();
        }
    }

    public async Task<System.Collections.Generic.List<int>> GetOrderIdsByShipmentIdAsync(int shipmentId, System.Data.IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        var conn = transaction?.Connection ?? _connectionFactory.CreateConnection();
        try
        {
            string sql = @""SELECT DISTINCT oi.OrderId FROM dbo.VcbShipmentItems si INNER JOIN dbo.VcbOrderItems oi ON si.OrderItemId = oi.Id WHERE si.ShipmentId = @Id"";
            var ids = await conn.QueryAsync<int>(sql, new { Id = shipmentId }, transaction: transaction);
            return new System.Collections.Generic.List<int>(ids);
        }
        finally
        {
            if (transaction == null) conn.Dispose();
        }
    }

    public async Task<System.Collections.Generic.List<int>> GetFulfilledOrderIdsAsync(int shipmentId, System.Data.IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        var conn = transaction?.Connection ?? _connectionFactory.CreateConnection();
        try
        {
            string sql = @""
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
                  );"";
            var ids = await conn.QueryAsync<int>(sql, new { CurrentShipmentId = shipmentId }, transaction: transaction);
            return new System.Collections.Generic.List<int>(ids);
        }
        finally
        {
            if (transaction == null) conn.Dispose();
        }
    }

    public async Task<int> GetBranchIdByOrderIdAsync(int orderId, System.Data.IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        var conn = transaction?.Connection ?? _connectionFactory.CreateConnection();
        try
        {
            string sql = ""SELECT TOP 1 BranchId FROM dbo.VcbOrders WHERE Id = @OrderId"";
            return await conn.QuerySingleOrDefaultAsync<int>(sql, new { OrderId = orderId }, transaction: transaction);
        }
        finally
        {
            if (transaction == null) conn.Dispose();
        }
    }
}
";

        File.WriteAllText(path, content + newMethods);
    }
}
