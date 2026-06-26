using Dapper;
using PCS_API.DTOs;

namespace PCS_API.Repositories;

public class VcbDeliveryRepository(ISqlConnectionFactory connectionFactory) : IVcbDeliveryRepository
{
    public async Task<VcbDeliveryDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        using var conn = connectionFactory.CreateConnection();
        string sql = @"
            SELECT d.*,
                   uc.Username AS CreatedByUsername,
                   uu.Username AS UpdatedByUsername
            FROM dbo.VcbDeliveries d
            LEFT JOIN dbo.Users uc ON d.CreatedBy = uc.Id
            LEFT JOIN dbo.Users uu ON d.UpdatedBy = uu.Id
            WHERE d.Id = @Id;
            
            SELECT do.DeliveryId, do.OrderId, o.OrderNo FROM dbo.VcbDeliveryOrders do
            INNER JOIN dbo.VcbOrders o ON do.OrderId = o.Id
            WHERE do.DeliveryId = @Id;

            SELECT Id, DeliveryId, PackageBoxNo, DomesticTrackingNo, TotalWeight, BoxDimensions, ContainedBoxNumbers, ShippingCost FROM dbo.VcbDeliveryItems WHERE DeliveryId = @Id;

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
        using var conn = connectionFactory.CreateConnection();
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
                          FOR XML PATH('')), 1, 2, '') AS OrderNumbers,
                   uc.Username AS CreatedByUsername,
                   uu.Username AS UpdatedByUsername
            FROM dbo.VcbDeliveries d
            LEFT JOIN dbo.Users uc ON d.CreatedBy = uc.Id
            LEFT JOIN dbo.Users uu ON d.UpdatedBy = uu.Id
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

    public async Task<int> CreateAsync(VcbDeliveryCreateDto dto, string? transferSlipUrl, int currentUserId, System.Data.IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        var conn = transaction?.Connection ?? connectionFactory.CreateConnection();
        try
        {
            string insertSql = @"
                INSERT INTO dbo.VcbDeliveries (
                    DeliveryNo, OrderDate, ShippingAddress, DomesticShippingCompany, 
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
            }, transaction: transaction, cancellationToken: cancellationToken));

            // Insert Delivery Orders mapping
            string insertOrdersSql = @"INSERT INTO dbo.VcbDeliveryOrders (DeliveryId, OrderId) VALUES (@DeliveryId, @OrderId)";
            var orderParams = dto.OrderIds.Select(oId => new { DeliveryId = deliveryId, OrderId = oId });
            await conn.ExecuteAsync(new CommandDefinition(insertOrdersSql, orderParams, transaction: transaction, cancellationToken: cancellationToken));

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
            await conn.ExecuteAsync(new CommandDefinition(insertItemsSql, itemParams, transaction: transaction, cancellationToken: cancellationToken));

            return deliveryId;
        }
        finally
        {
            if (transaction == null) conn.Dispose();
        }
    }

    public async Task<bool> UpdateAsync(int id, VcbDeliveryCreateDto dto, string? transferSlipUrl, int currentUserId, System.Data.IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        var conn = transaction?.Connection ?? connectionFactory.CreateConnection();
        try
        {
            var existing = await conn.QuerySingleOrDefaultAsync<string>("SELECT TransferSlipUrl FROM dbo.VcbDeliveries WHERE Id = @Id", new { Id = id }, transaction: transaction);
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
                    UpdatedBy = @UpdatedBy,
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
                dto.Notes,
                UpdatedBy = currentUserId
            }, transaction: transaction, cancellationToken: cancellationToken));

            await conn.ExecuteAsync(new CommandDefinition("DELETE FROM dbo.VcbDeliveryOrders WHERE DeliveryId = @Id", new { Id = id }, transaction: transaction, cancellationToken: cancellationToken));
            string insertOrdersSql = @"INSERT INTO dbo.VcbDeliveryOrders (DeliveryId, OrderId) VALUES (@DeliveryId, @OrderId)";
            if (dto.OrderIds != null && dto.OrderIds.Any())
            {
                var orderParams = dto.OrderIds.Select(oId => new { DeliveryId = id, OrderId = oId });
                await conn.ExecuteAsync(new CommandDefinition(insertOrdersSql, orderParams, transaction: transaction, cancellationToken: cancellationToken));
            }

            await conn.ExecuteAsync(new CommandDefinition("DELETE FROM dbo.VcbDeliveryItems WHERE DeliveryId = @Id", new { Id = id }, transaction: transaction, cancellationToken: cancellationToken));
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
                await conn.ExecuteAsync(new CommandDefinition(insertItemsSql, itemParams, transaction: transaction, cancellationToken: cancellationToken));
            }

            return true;
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
            string sql = "UPDATE dbo.VcbDeliveries SET Status = @Status, UpdatedBy = @UpdatedBy, UpdatedAt = GETDATE() WHERE Id = @Id";
            int rows = await conn.ExecuteAsync(new CommandDefinition(sql, new { Id = id, Status = status, UpdatedBy = currentUserId }, transaction: transaction, cancellationToken: cancellationToken));
            return rows > 0;
        }
        finally
        {
            if (transaction == null) conn.Dispose();
        }
    }

    public async Task<IEnumerable<string>> GetDeliveryBoxesAsync(int deliveryId, System.Data.IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        var conn = transaction?.Connection ?? connectionFactory.CreateConnection();
        try
        {
            string sql = "SELECT ContainedBoxNumbers FROM dbo.VcbDeliveryItems WHERE DeliveryId = @DeliveryId;";
            return await conn.QueryAsync<string>(new CommandDefinition(sql, new { DeliveryId = deliveryId }, transaction: transaction, cancellationToken: cancellationToken));
        }
        finally
        {
            if (transaction == null) conn.Dispose();
        }
    }
}