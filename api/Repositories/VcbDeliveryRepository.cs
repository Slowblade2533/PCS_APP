using Dapper;
using Microsoft.Data.SqlClient;
using PCS_API.DTOs;
using PCS_API.Models;

namespace PCS_API.Repositories;

public class VcbDeliveryRepository : IVcbDeliveryRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public VcbDeliveryRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<VcbDeliveryDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        string sql = @"
            SELECT * FROM dbo.VcbDeliveries WHERE Id = @Id;
            
            SELECT o.*, o.Id as OrderId FROM dbo.VcbDeliveryOrders do
            INNER JOIN dbo.VcbOrders o ON do.OrderId = o.Id
            WHERE do.DeliveryId = @Id;

            SELECT * FROM dbo.VcbDeliveryItems WHERE DeliveryId = @Id;

            SELECT si.BoxNumbers 
            FROM dbo.VcbShipmentItems si
            INNER JOIN dbo.VcbShipments s ON si.ShipmentId = s.Id
            WHERE s.DeliveryId = @Id AND s.Status = 'Completed';";

        using var multi = await conn.QueryMultipleAsync(new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));
        var delivery = await multi.ReadFirstOrDefaultAsync<VcbDeliveryDto>();
        if (delivery == null) return null;

        delivery.Orders = (await multi.ReadAsync<VcbDeliveryOrderDto>()).ToList();
        delivery.Items = (await multi.ReadAsync<VcbDeliveryItemDto>()).ToList();

        var receivedBoxStrings = (await multi.ReadAsync<string>()).ToList();
        var receivedBoxesList = new List<string>();
        foreach (var boxStr in receivedBoxStrings)
        {
            if (string.IsNullOrEmpty(boxStr)) continue;
            var parts = boxStr.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                if (!receivedBoxesList.Contains(trimmed))
                {
                    receivedBoxesList.Add(trimmed);
                }
            }
        }
        delivery.ReceivedBoxNumbers = receivedBoxesList;

        return delivery;
    }

    public async Task<PagedResultDto<VcbDeliveryDto>> GetPagedAsync(VcbDeliverySearchDto search, CancellationToken cancellationToken = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        var parameters = new DynamicParameters();
        string whereClause = "WHERE 1=1";

        if (!string.IsNullOrEmpty(search.SearchTerm))
        {
            whereClause += @" AND (d.DeliveryNo LIKE @SearchTerm OR d.DomesticShippingCompany LIKE @SearchTerm 
                                  OR EXISTS (SELECT 1 FROM dbo.VcbDeliveryItems i WHERE i.DeliveryId = d.Id AND (i.PackageBoxNo LIKE @SearchTerm OR i.ContainedBoxNumbers LIKE @SearchTerm))
                                  OR EXISTS (SELECT 1 FROM dbo.VcbDeliveryOrders do INNER JOIN dbo.VcbOrders o ON do.OrderId = o.Id WHERE do.DeliveryId = d.Id AND o.OrderNo LIKE @SearchTerm))";
            parameters.Add("SearchTerm", $"%{search.SearchTerm}%");
        }

        if (!string.IsNullOrEmpty(search.Status))
        {
            whereClause += " AND d.Status = @Status";
            parameters.Add("Status", search.Status);
        }

        string sql = $@"
            SELECT COUNT(*) FROM dbo.VcbDeliveries d {whereClause};

            SELECT d.*, 
                   STUFF((SELECT ', ' + i.PackageBoxNo 
                          FROM dbo.VcbDeliveryItems i 
                          WHERE i.DeliveryId = d.Id 
                          FOR XML PATH('')), 1, 2, '') AS PackageBoxes,
                   STUFF((SELECT ', ' + o.OrderNo 
                          FROM dbo.VcbDeliveryOrders do 
                          INNER JOIN dbo.VcbOrders o ON do.OrderId = o.Id
                          WHERE do.DeliveryId = d.Id 
                          FOR XML PATH('')), 1, 2, '') AS OrderNumbers
            FROM dbo.VcbDeliveries d
            {whereClause}
            ORDER BY d.OrderDate DESC, d.Id DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

        parameters.Add("Offset", search.GetSafeOffset());
        parameters.Add("PageSize", search.PageSize);

        var command = new CommandDefinition(sql, parameters, cancellationToken: cancellationToken);
        using var multi = await conn.QueryMultipleAsync(command);
        int totalCount = await multi.ReadSingleAsync<int>();
        var items = await multi.ReadAsync<VcbDeliveryDto>();

        return new PagedResultDto<VcbDeliveryDto>
        {
            Items = items.ToList(),
            TotalCount = totalCount,
            PageNumber = search.PageNumber,
            PageSize = search.PageSize
        };
    }

    public async Task<int> CreateAsync(VcbDeliveryCreateDto dto, string? transferSlipUrl, int currentUserId, CancellationToken cancellationToken = default)
    {
        using var conn = _connectionFactory.CreateConnection() as System.Data.Common.DbConnection;
        if (conn == null) throw new InvalidOperationException("Could not create DbConnection.");
        await conn.OpenAsync(cancellationToken);
        using var tx = await conn.BeginTransactionAsync(cancellationToken);

        try
        {
            string insertSql = @"
                INSERT INTO dbo.VcbDeliveries (DeliveryNo, OrderDate, ShippingAddress, DomesticShippingCompany, 
                    TotalAmountBeforeDiscount, DiscountAmount, TotalAmount, TransferredAmount, TransferSlipUrl, 
                    Status, Notes, CreatedBy, CreatedAt, UpdatedAt)
                VALUES (@DeliveryNo, @OrderDate, @ShippingAddress, @DomesticShippingCompany, 
                    @TotalAmountBeforeDiscount, @DiscountAmount, @TotalAmount, @TransferredAmount, @TransferSlipUrl, 
                    'Shipping', @Notes, @CreatedBy, GETDATE(), GETDATE());
                SELECT CAST(SCOPE_IDENTITY() as int);";

            int deliveryId = await conn.QuerySingleAsync<int>(new CommandDefinition(insertSql, new
            {
                dto.DeliveryNo,
                dto.OrderDate,
                dto.ShippingAddress,
                dto.DomesticShippingCompany,
                dto.TotalAmountBeforeDiscount,
                dto.DiscountAmount,
                dto.TotalAmount,
                dto.TransferredAmount,
                TransferSlipUrl = transferSlipUrl,
                dto.Notes,
                CreatedBy = currentUserId
            }, transaction: tx, cancellationToken: cancellationToken));

            // Insert Delivery Orders mapping
            string insertOrdersSql = @"INSERT INTO dbo.VcbDeliveryOrders (DeliveryId, OrderId) VALUES (@DeliveryId, @OrderId)";
            var orderParams = dto.OrderIds.Select(oId => new { DeliveryId = deliveryId, OrderId = oId });
            await conn.ExecuteAsync(new CommandDefinition(insertOrdersSql, orderParams, transaction: tx, cancellationToken: cancellationToken));

            // Insert Delivery Items
            string insertItemsSql = @"
                INSERT INTO dbo.VcbDeliveryItems (DeliveryId, PackageBoxNo, DomesticTrackingNo, TotalWeight, BoxDimensions, ContainedBoxNumbers, ShippingCost)
                VALUES (@DeliveryId, @PackageBoxNo, @DomesticTrackingNo, @TotalWeight, @BoxDimensions, @ContainedBoxNumbers, @ShippingCost)";
            
            var itemParams = dto.Items.Select(i => new
            {
                DeliveryId = deliveryId,
                i.PackageBoxNo,
                i.DomesticTrackingNo,
                i.TotalWeight,
                i.BoxDimensions,
                i.ContainedBoxNumbers,
                i.ShippingCost
            });
            await conn.ExecuteAsync(new CommandDefinition(insertItemsSql, itemParams, transaction: tx, cancellationToken: cancellationToken));

            // Create Expense AccountTransaction
            if (dto.TransferredAmount > 0)
            {
                string insertExpenseSql = @"
                    INSERT INTO dbo.AccountTransactions (TransactionDate, Type, Amount, ReferenceType, ReferenceId, Notes, CreatedBy, CreatedAt)
                    VALUES (GETDATE(), 'Expense', @Amount, 'VcbDeliveryCost', @ReferenceId, N'ค่าขนส่ง VCANBUY บิล: ' + @DeliveryNo, @CreatedBy, GETDATE());";
                await conn.ExecuteAsync(new CommandDefinition(insertExpenseSql, new
                {
                    Amount = dto.TransferredAmount,
                    ReferenceId = deliveryId,
                    dto.DeliveryNo,
                    CreatedBy = currentUserId
                }, transaction: tx, cancellationToken: cancellationToken));
            }

            await tx.CommitAsync(cancellationToken);
            return deliveryId;
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<bool> UpdateAsync(int id, VcbDeliveryCreateDto dto, string? transferSlipUrl, int currentUserId, CancellationToken cancellationToken = default)
    {
        using var conn = _connectionFactory.CreateConnection() as System.Data.Common.DbConnection;
        if (conn == null) throw new InvalidOperationException("Could not create DbConnection.");
        await conn.OpenAsync(cancellationToken);
        using var tx = await conn.BeginTransactionAsync(cancellationToken);

        try
        {
            var existing = await conn.QuerySingleOrDefaultAsync<string>("SELECT TransferSlipUrl FROM dbo.VcbDeliveries WHERE Id = @Id", new { Id = id }, transaction: tx);
            if (existing == null) return false;

            string updateSql = @"
                UPDATE dbo.VcbDeliveries SET 
                    DeliveryNo = @DeliveryNo,
                    OrderDate = @OrderDate,
                    ShippingAddress = @ShippingAddress,
                    DomesticShippingCompany = @DomesticShippingCompany,
                    TotalAmountBeforeDiscount = @TotalAmountBeforeDiscount,
                    DiscountAmount = @DiscountAmount,
                    TotalAmount = @TotalAmount,
                    TransferredAmount = @TransferredAmount,
                    " + (transferSlipUrl != null ? "TransferSlipUrl = @TransferSlipUrl, " : "") + @"
                    Notes = @Notes,
                    UpdatedAt = GETDATE()
                WHERE Id = @Id";

            await conn.ExecuteAsync(new CommandDefinition(updateSql, new
            {
                Id = id,
                dto.DeliveryNo,
                dto.OrderDate,
                dto.ShippingAddress,
                dto.DomesticShippingCompany,
                dto.TotalAmountBeforeDiscount,
                dto.DiscountAmount,
                dto.TotalAmount,
                dto.TransferredAmount,
                TransferSlipUrl = transferSlipUrl,
                dto.Notes
            }, transaction: tx, cancellationToken: cancellationToken));

            await conn.ExecuteAsync(new CommandDefinition("DELETE FROM dbo.VcbDeliveryOrders WHERE DeliveryId = @Id", new { Id = id }, transaction: tx, cancellationToken: cancellationToken));
            string insertOrdersSql = @"INSERT INTO dbo.VcbDeliveryOrders (DeliveryId, OrderId) VALUES (@DeliveryId, @OrderId)";
            if (dto.OrderIds != null && dto.OrderIds.Any())
            {
                var orderParams = dto.OrderIds.Select(oId => new { DeliveryId = id, OrderId = oId });
                await conn.ExecuteAsync(new CommandDefinition(insertOrdersSql, orderParams, transaction: tx, cancellationToken: cancellationToken));
            }

            await conn.ExecuteAsync(new CommandDefinition("DELETE FROM dbo.VcbDeliveryItems WHERE DeliveryId = @Id", new { Id = id }, transaction: tx, cancellationToken: cancellationToken));
            string insertItemsSql = @"
                INSERT INTO dbo.VcbDeliveryItems (DeliveryId, PackageBoxNo, DomesticTrackingNo, TotalWeight, BoxDimensions, ContainedBoxNumbers, ShippingCost)
                VALUES (@DeliveryId, @PackageBoxNo, @DomesticTrackingNo, @TotalWeight, @BoxDimensions, @ContainedBoxNumbers, @ShippingCost)";
            if (dto.Items != null && dto.Items.Any())
            {
                var itemParams = dto.Items.Select(i => new
                {
                    DeliveryId = id,
                    i.PackageBoxNo,
                    i.DomesticTrackingNo,
                    i.TotalWeight,
                    i.BoxDimensions,
                    i.ContainedBoxNumbers,
                    i.ShippingCost
                });
                await conn.ExecuteAsync(new CommandDefinition(insertItemsSql, itemParams, transaction: tx, cancellationToken: cancellationToken));
            }

            var existingExpense = await conn.QuerySingleOrDefaultAsync<int?>("SELECT Id FROM dbo.AccountTransactions WHERE ReferenceType = 'VcbDeliveryCost' AND ReferenceId = @Id", new { Id = id }, transaction: tx);
            if (dto.TransferredAmount > 0)
            {
                if (existingExpense.HasValue)
                {
                    string updateExpenseSql = "UPDATE dbo.AccountTransactions SET Amount = @Amount, Notes = N'ค่าขนส่ง VCANBUY บิล: ' + @DeliveryNo WHERE Id = @ExpenseId";
                    await conn.ExecuteAsync(new CommandDefinition(updateExpenseSql, new { Amount = dto.TransferredAmount, dto.DeliveryNo, ExpenseId = existingExpense.Value }, transaction: tx, cancellationToken: cancellationToken));
                }
                else
                {
                    string insertExpenseSql = @"
                        INSERT INTO dbo.AccountTransactions (TransactionDate, Type, Amount, ReferenceType, ReferenceId, Notes, CreatedBy, CreatedAt)
                        VALUES (GETDATE(), 'Expense', @Amount, 'VcbDeliveryCost', @ReferenceId, N'ค่าขนส่ง VCANBUY บิล: ' + @DeliveryNo, @CreatedBy, GETDATE());";
                    await conn.ExecuteAsync(new CommandDefinition(insertExpenseSql, new
                    {
                        Amount = dto.TransferredAmount,
                        ReferenceId = id,
                        dto.DeliveryNo,
                        CreatedBy = currentUserId
                    }, transaction: tx, cancellationToken: cancellationToken));
                }
            }
            else
            {
                if (existingExpense.HasValue)
                {
                    await conn.ExecuteAsync(new CommandDefinition("DELETE FROM dbo.AccountTransactions WHERE Id = @ExpenseId", new { ExpenseId = existingExpense.Value }, transaction: tx, cancellationToken: cancellationToken));
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

    public async Task<bool> UpdateStatusAsync(int id, string status, int currentUserId, CancellationToken cancellationToken = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        string sql = "UPDATE dbo.VcbDeliveries SET Status = @Status, UpdatedAt = GETDATE() WHERE Id = @Id";
        int rows = await conn.ExecuteAsync(new CommandDefinition(sql, new { Id = id, Status = status }, cancellationToken: cancellationToken));
        return rows > 0;
    }
}
