using Dapper;
using Microsoft.Data.SqlClient;
using PCS_API.DTOs;
using System.Text;

namespace PCS_API.Repositories;

public class VcbOrderRepository : IVcbOrderRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public VcbOrderRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<VcbOrderDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        string sql = @"
            SELECT o.Id, o.OrderNo, o.OrderDate, o.TotalAmount, o.Status, o.BranchId,
                   o.Notes, o.TransferSlipUrl, o.CreatedBy, o.CreatedAt, o.UpdatedAt
            FROM dbo.VcbOrders o
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
        using var conn = _connectionFactory.CreateConnection();
        var parameters = new DynamicParameters();
        string whereClause = "WHERE 1=1";

        if (!string.IsNullOrEmpty(search.SearchTerm))
        {
            whereClause += " AND (o.OrderNo LIKE @SearchTerm OR CONVERT(VARCHAR, o.OrderDate, 120) LIKE @SearchTerm)";
            parameters.Add("SearchTerm", $"%{search.SearchTerm}%");
        }

        if (!string.IsNullOrEmpty(search.Status))
        {
            whereClause += " AND o.Status = @Status";
            parameters.Add("Status", search.Status);
        }

        if (search.BranchId.HasValue)
        {
            whereClause += " AND o.BranchId = @BranchId";
            parameters.Add("BranchId", search.BranchId.Value);
        }

        string sql = $@"
            SELECT COUNT(*) FROM dbo.VcbOrders o {whereClause};

            SELECT o.*, 
                   (SELECT COUNT(*) FROM dbo.VcbOrderItems i WHERE i.OrderId = o.Id) AS ItemCount 
            FROM dbo.VcbOrders o
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
        using var conn = _connectionFactory.CreateConnection() as System.Data.Common.DbConnection;
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
                var batchSql = new StringBuilder();
                var batchParams = new DynamicParameters();
                batchParams.Add("OrderId", orderId);

                for (int i = 0; i < dto.Items.Count; i++)
                {
                    var item = dto.Items[i];
                    batchSql.AppendLine($@"
                        INSERT INTO dbo.VcbOrderItems (OrderId, VariantId, Quantity, TotalPrice)
                        VALUES (@OrderId, @VariantId{i}, @Quantity{i}, @TotalPrice{i});");
                    batchParams.Add($"VariantId{i}", item.VariantId);
                    batchParams.Add($"Quantity{i}", item.Quantity);
                    batchParams.Add($"TotalPrice{i}", item.TotalPrice);
                }

                await conn.ExecuteAsync(new CommandDefinition(batchSql.ToString(), batchParams, transaction: tx, cancellationToken: cancellationToken));
            }

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
        using var conn = _connectionFactory.CreateConnection() as System.Data.Common.DbConnection;
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
                TransferSlipUrl = transferSlipUrl
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
                var batchSql = new StringBuilder();
                var batchParams = new DynamicParameters();
                batchParams.Add("OrderId", id);

                for (int i = 0; i < dto.Items.Count; i++)
                {
                    var item = dto.Items[i];
                    batchSql.AppendLine($@"
                        INSERT INTO dbo.VcbOrderItems (OrderId, VariantId, Quantity, TotalPrice)
                        VALUES (@OrderId, @VariantId{i}, @Quantity{i}, @TotalPrice{i});");
                    batchParams.Add($"VariantId{i}", item.VariantId);
                    batchParams.Add($"Quantity{i}", item.Quantity);
                    batchParams.Add($"TotalPrice{i}", item.TotalPrice);
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

    public async Task<bool> UpdateStatusAsync(int id, string status, CancellationToken cancellationToken = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        string sql = @"
            UPDATE dbo.VcbOrders
            SET Status = @Status,
                UpdatedAt = GETDATE()
            WHERE Id = @Id;";
        
        int rows = await conn.ExecuteAsync(new CommandDefinition(sql, new { Id = id, Status = status }, cancellationToken: cancellationToken));
        return rows > 0;
    }
}
