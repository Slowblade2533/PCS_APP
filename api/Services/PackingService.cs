using PCS_API.DTOs;
using PCS_API.Repositories;
using System.Data;

namespace PCS_API.Services;

public class PackingService(
    IPackingRepository repo,
    ISqlConnectionFactory connectionFactory,
    ILogger<PackingService> logger) : IPackingService
{
    public async Task<PackingBatchResponseDto> CreateDraftBatchAsync(CreatePackingBatchDto dto, int? userId, CancellationToken cancellationToken = default)
    {
        using var conn = connectionFactory.CreateConnection() as System.Data.Common.DbConnection;
        if (conn == null) throw new InvalidOperationException("Could not create DbConnection.");
        await conn.OpenAsync(cancellationToken);
        using var dbTransaction = await conn.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);

        try
        {
            // 1. Create Batch Header
            int batchId = await repo.CreateBatchAsync(dto, userId, dbTransaction);

            // 2. Process Items and lookup VariantId if not supplied
            foreach (var item in dto.Items)
            {
                if (!item.VariantId.HasValue && !string.IsNullOrWhiteSpace(item.Sku))
                {
                    item.VariantId = await repo.FindVariantIdBySkuAsync(item.Sku.Trim(), dbTransaction);
                    item.IsSkuMatched = item.VariantId.HasValue;
                }

                await repo.CreateBatchItemAsync(batchId, item, dbTransaction);
            }

            await dbTransaction.CommitAsync(cancellationToken);

            var createdBatch = await repo.GetBatchByIdAsync(batchId, cancellationToken);
            return createdBatch!;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating draft packing batch.");
            await dbTransaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<Dictionary<string, SkuCheckResultDto>> CheckSkusAsync(IEnumerable<string> skus, CancellationToken cancellationToken = default)
    {
        var foundDict = await repo.CheckSkusExistAsync(skus, cancellationToken);
        var result = new Dictionary<string, SkuCheckResultDto>(StringComparer.OrdinalIgnoreCase);

        foreach (var sku in skus)
        {
            if (string.IsNullOrWhiteSpace(sku)) continue;
            string key = sku.Trim();

            if (foundDict.TryGetValue(key, out var match))
            {
                result[key] = new SkuCheckResultDto
                {
                    IsMatched = true,
                    VariantId = match.VariantId,
                    VariantName = match.VariantName
                };
            }
            else
            {
                result[key] = new SkuCheckResultDto
                {
                    IsMatched = false,
                    VariantId = null,
                    VariantName = null
                };
            }
        }

        return result;
    }

    public async Task<PagedResultDto<PackingBatchResponseDto>> GetBatchesPagedAsync(PaginationParamsDto @params, CancellationToken cancellationToken = default)
    {
        return await repo.GetBatchesPagedAsync(@params, cancellationToken);
    }

    public async Task<PackingBatchResponseDto?> GetBatchByIdAsync(int batchId, CancellationToken cancellationToken = default)
    {
        return await repo.GetBatchByIdAsync(batchId, cancellationToken);
    }

    public async Task<bool> ConfirmShipmentAsync(ConfirmShipmentDto dto, int? userId, CancellationToken cancellationToken = default)
    {
        using var conn = connectionFactory.CreateConnection() as System.Data.Common.DbConnection;
        if (conn == null) throw new InvalidOperationException("Could not create DbConnection.");
        await conn.OpenAsync(cancellationToken);
        using var dbTransaction = await conn.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);

        try
        {
            foreach (var shipItem in dto.Items)
            {
                if (shipItem.ShippedQuantity <= 0) continue;

                const string getItemSql = @"
                    SELECT [Id], [BatchId], [Platform], [OrderNo], [TrackingNo], [Sku], [VariantId], 
                           [OrderedQuantity], [ShippedQuantity], [PackStatus]
                    FROM [dbo].[PackingBatchItems]
                    WHERE [Id] = @Id;";

                using var cmd = conn.CreateCommand();
                cmd.Transaction = dbTransaction;
                cmd.CommandText = getItemSql;
                var param = cmd.CreateParameter();
                param.ParameterName = "@Id";
                param.Value = shipItem.BatchItemId;
                cmd.Parameters.Add(param);

                using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
                if (!await reader.ReadAsync(cancellationToken)) continue;

                int variantId = reader.IsDBNull(reader.GetOrdinal("VariantId")) ? 0 : reader.GetInt32(reader.GetOrdinal("VariantId"));
                string trackingNo = reader.GetString(reader.GetOrdinal("TrackingNo"));
                string orderNo = reader.GetString(reader.GetOrdinal("OrderNo"));
                string platform = reader.GetString(reader.GetOrdinal("Platform"));
                int orderedQty = reader.GetInt32(reader.GetOrdinal("OrderedQuantity"));
                int currentShipped = reader.GetInt32(reader.GetOrdinal("ShippedQuantity"));
                reader.Close();

                int newShippedQty = currentShipped + shipItem.ShippedQuantity;
                string newStatus = newShippedQty >= orderedQty ? "SHIPPED" : "PARTIAL_SHIPPED";

                await repo.UpdateItemShippedQuantityAsync(shipItem.BatchItemId, shipItem.ShippedQuantity, newStatus, dbTransaction);

                if (variantId > 0)
                {
                    string notes = $"ตัดสต๊อกจัดส่งสินค้าจากแพลตฟอร์ม {platform} (ออเดอร์ #{orderNo}, Tracking: {trackingNo})";
                    await repo.DeductStockForShipmentAsync(variantId, shipItem.ShippedQuantity, trackingNo, notes, userId, dbTransaction);
                }
            }

            await dbTransaction.CommitAsync(cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error confirming shipment for packing batch items.");
            await dbTransaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<List<PackingBatchItemResponseDto>> GetPendingBackordersAsync(CancellationToken cancellationToken = default)
    {
        return await repo.GetPendingBackordersAsync(cancellationToken);
    }

    public async Task<bool> ResolveBackorderAsync(ResolveBackorderDto dto, int? userId, CancellationToken cancellationToken = default)
    {
        using var conn = connectionFactory.CreateConnection() as System.Data.Common.DbConnection;
        if (conn == null) throw new InvalidOperationException("Could not create DbConnection.");
        await conn.OpenAsync(cancellationToken);
        using var dbTransaction = await conn.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);

        try
        {
            await repo.CreatePendingResolutionAsync(dto, userId, dbTransaction);

            if (dto.ActualShippedVariantId > 0 && dto.ShippedQuantity > 0 && dto.ResolutionType != "REFUND_CANCEL")
            {
                string notes = $"จัดส่งสินค้าทดแทนตามหลัง (Resolution: {dto.ResolutionType}) Note: {dto.CustomerAgreementNote}";
                string refDoc = dto.FollowUpTrackingNo ?? "BACKORDER-RESOLVE";
                await repo.DeductStockForShipmentAsync(dto.ActualShippedVariantId, dto.ShippedQuantity, refDoc, notes, userId, dbTransaction);
            }

            await repo.UpdateItemShippedQuantityAsync(dto.BatchItemId, dto.ShippedQuantity, "RESOLVED", dbTransaction);

            await dbTransaction.CommitAsync(cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error resolving backorder for BatchItemId={BatchItemId}", dto.BatchItemId);
            await dbTransaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
