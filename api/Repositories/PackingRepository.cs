using Dapper;
using PCS_API.DTOs;
using System.Data;
using System.Text.RegularExpressions;

namespace PCS_API.Repositories;

public class PackingRepository(ISqlConnectionFactory connectionFactory) : IPackingRepository
{
    private static string CleanSkuPrefix(string sku)
    {
        if (string.IsNullOrWhiteSpace(sku)) return string.Empty;
        // Strip store tag prefix like [Lz01], [Sp01], [Lz02] etc.
        return Regex.Replace(sku.Trim(), @"^\[[A-Za-z0-9_-]+\]\s*", "", RegexOptions.IgnoreCase).Trim();
    }

    private static string NormalizeString(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        // Replace non-breaking spaces (\u00A0) with standard space and trim
        return Regex.Replace(input.Replace('\u00A0', ' ').Trim(), @"\s+", " ").ToLowerInvariant();
    }

    public async Task<int> CreateBatchAsync(CreatePackingBatchDto dto, int? userId, IDbTransaction? transaction = null)
    {
        var conn = transaction?.Connection ?? connectionFactory.CreateConnection();
        string batchNo = dto.BatchNo ?? $"PACK-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";

        const string sql = @"
            INSERT INTO [dbo].[PackingBatches]
                ([BatchNo], [ImportedAt], [ImportedBy], [TotalFiles], [TotalOrders], [TotalParcels], [TotalItems], [Status], [Notes])
            VALUES
                (@BatchNo, SYSUTCDATETIME(), @ImportedBy, @TotalFiles, @TotalOrders, @TotalParcels, @TotalItems, 'DRAFT', @Notes);
            SELECT CAST(SCOPE_IDENTITY() as int);";

        return await conn.ExecuteScalarAsync<int>(sql, new
        {
            BatchNo = batchNo,
            ImportedBy = userId,
            dto.TotalFiles,
            dto.TotalOrders,
            dto.TotalParcels,
            dto.TotalItems,
            dto.Notes
        }, transaction);
    }

    public async Task CreateBatchItemAsync(int batchId, PackingBatchItemCreateDto item, IDbTransaction? transaction = null)
    {
        var conn = transaction?.Connection ?? connectionFactory.CreateConnection();

        const string sql = @"
            INSERT INTO [dbo].[PackingBatchItems]
                ([BatchId], [Platform], [OrderNo], [TrackingNo], [ShippingProvider], [Sku], [VariantId], [ProductName], 
                 [OrderedQuantity], [ShippedQuantity], [OrderDate], [IsMultiParcel], [ParcelSeq], [TotalParcelsInOrder], 
                 [PackStatus], [IsSkuMatched])
            VALUES
                (@BatchId, @Platform, @OrderNo, @TrackingNo, @ShippingProvider, @Sku, @VariantId, @ProductName, 
                 @OrderedQuantity, @ShippedQuantity, @OrderDate, @IsMultiParcel, @ParcelSeq, @TotalParcelsInOrder, 
                 'DRAFT', @IsSkuMatched);";

        await conn.ExecuteAsync(sql, new
        {
            BatchId = batchId,
            item.Platform,
            item.OrderNo,
            item.TrackingNo,
            item.ShippingProvider,
            item.Sku,
            item.VariantId,
            item.ProductName,
            item.OrderedQuantity,
            item.ShippedQuantity,
            item.OrderDate,
            item.IsMultiParcel,
            item.ParcelSeq,
            item.TotalParcelsInOrder,
            item.IsSkuMatched
        }, transaction);
    }

    public async Task<int?> FindVariantIdBySkuAsync(string sku, IDbTransaction? transaction = null)
    {
        var conn = transaction?.Connection ?? connectionFactory.CreateConnection();
        string cleaned = CleanSkuPrefix(sku);
        string normRaw = NormalizeString(sku);
        string normCleaned = NormalizeString(cleaned);

        const string sql = @"
            SELECT pv.[VariantId], pv.[Sku], pv.[Barcode], pv.[VariantNameTh], p.[ProductNameTh]
            FROM [dbo].[ProductVariants] pv
            LEFT JOIN [dbo].[Products] p ON pv.[ProductId] = p.[ProductId];";

        var allVariants = await conn.QueryAsync(sql, transaction: transaction);
        var match = allVariants.FirstOrDefault(v =>
            NormalizeString(v.Sku) == normRaw ||
            NormalizeString(v.Sku) == normCleaned ||
            NormalizeString(v.Barcode) == normRaw ||
            NormalizeString(v.Barcode) == normCleaned ||
            NormalizeString(v.VariantNameTh) == normRaw ||
            NormalizeString(v.VariantNameTh) == normCleaned ||
            NormalizeString(v.ProductNameTh) == normRaw ||
            NormalizeString(v.ProductNameTh) == normCleaned
        );

        return match?.VariantId;
    }

    public async Task<Dictionary<string, (int VariantId, string VariantName)>> CheckSkusExistAsync(IEnumerable<string> skus, CancellationToken cancellationToken = default)
    {
        using var conn = connectionFactory.CreateConnection();
        var rawList = skus.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).Distinct().ToList();
        if (!rawList.Any()) return new(StringComparer.OrdinalIgnoreCase);

        const string sql = @"
            SELECT pv.[VariantId], pv.[Sku], pv.[Barcode], pv.[VariantNameTh], p.[ProductNameTh],
                   COALESCE(NULLIF(pv.[VariantNameTh], ''), NULLIF(p.[ProductNameTh], ''), pv.[Sku]) AS DisplayName
            FROM [dbo].[ProductVariants] pv
            LEFT JOIN [dbo].[Products] p ON pv.[ProductId] = p.[ProductId];";

        var allVariants = (await conn.QueryAsync(sql)).ToList();
        var dict = new Dictionary<string, (int VariantId, string VariantName)>(StringComparer.OrdinalIgnoreCase);

        foreach (var rawSku in rawList)
        {
            string cleaned = CleanSkuPrefix(rawSku);
            string normRaw = NormalizeString(rawSku);
            string normCleaned = NormalizeString(cleaned);

            // Robust multi-column normalized matching
            var match = allVariants.FirstOrDefault(v =>
                NormalizeString(v.Sku) == normRaw ||
                NormalizeString(v.Sku) == normCleaned ||
                NormalizeString(v.Barcode) == normRaw ||
                NormalizeString(v.Barcode) == normCleaned ||
                NormalizeString(v.VariantNameTh) == normRaw ||
                NormalizeString(v.VariantNameTh) == normCleaned ||
                NormalizeString(v.ProductNameTh) == normRaw ||
                NormalizeString(v.ProductNameTh) == normCleaned ||
                (!string.IsNullOrEmpty(normCleaned) && NormalizeString(v.VariantNameTh).Contains(normCleaned)) ||
                (!string.IsNullOrEmpty(normCleaned) && normCleaned.Contains(NormalizeString(v.VariantNameTh)))
            );

            if (match != null)
            {
                dict[rawSku] = (match.VariantId, match.DisplayName);
            }
        }

        return dict;
    }

    public async Task<PagedResultDto<PackingBatchResponseDto>> GetBatchesPagedAsync(PaginationParamsDto @params, CancellationToken cancellationToken = default)
    {
        using var conn = connectionFactory.CreateConnection();
        int offset = @params.GetSafeOffset();

        const string countSql = "SELECT COUNT(*) FROM [dbo].[PackingBatches];";
        int totalItems = await conn.ExecuteScalarAsync<int>(countSql);

        const string dataSql = @"
            SELECT [BatchId], [BatchNo], [ImportedAt], [ImportedBy], [TotalFiles], [TotalOrders], 
                   [TotalParcels], [TotalItems], [Status], [Notes]
            FROM [dbo].[PackingBatches]
            ORDER BY [ImportedAt] DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

        var batches = (await conn.QueryAsync<PackingBatchResponseDto>(dataSql, new { Offset = offset, PageSize = @params.PageSize })).ToList();

        return new PagedResultDto<PackingBatchResponseDto>
        {
            Items = batches,
            TotalCount = totalItems,
            PageNumber = @params.PageNumber,
            PageSize = @params.PageSize
        };
    }

    public async Task<PackingBatchResponseDto?> GetBatchByIdAsync(int batchId, CancellationToken cancellationToken = default)
    {
        using var conn = connectionFactory.CreateConnection();

        const string batchSql = @"
            SELECT [BatchId], [BatchNo], [ImportedAt], [ImportedBy], [TotalFiles], [TotalOrders], 
                   [TotalParcels], [TotalItems], [Status], [Notes]
            FROM [dbo].[PackingBatches]
            WHERE [BatchId] = @BatchId;";

        var batch = await conn.QueryFirstOrDefaultAsync<PackingBatchResponseDto>(batchSql, new { BatchId = batchId });
        if (batch == null) return null;

        const string itemsSql = @"
            SELECT [Id], [BatchId], [Platform], [OrderNo], [TrackingNo], [ShippingProvider], 
                   [Sku], [VariantId], [ProductName], [OrderedQuantity], [ShippedQuantity], 
                   [OrderDate], [IsMultiParcel], [ParcelSeq], [TotalParcelsInOrder], 
                   [PackStatus], [IsSkuMatched]
            FROM [dbo].[PackingBatchItems]
            WHERE [BatchId] = @BatchId
            ORDER BY [OrderNo], [TrackingNo];";

        var items = (await conn.QueryAsync<PackingBatchItemResponseDto>(itemsSql, new { BatchId = batchId })).ToList();
        batch.Items = items;

        return batch;
    }

    public async Task DeductStockForShipmentAsync(int variantId, int quantity, string referenceDoc, string notes, int? userId, IDbTransaction? transaction = null)
    {
        var conn = transaction?.Connection ?? connectionFactory.CreateConnection();

        const string stockSql = @"
            UPDATE [dbo].[Stocks]
            SET [CurrentQuantity] = [CurrentQuantity] - @Quantity,
                [UpdatedAt] = SYSUTCDATETIME()
            WHERE [VariantId] = @VariantId AND [Condition] = 'NEW';";

        await conn.ExecuteAsync(stockSql, new { VariantId = variantId, Quantity = quantity }, transaction);

        const string txSql = @"
            INSERT INTO [dbo].[StockTransactions]
                ([VariantId], [TransactionType], [Quantity], [ReferenceDoc], [Notes], [CreatedAt], [CreatedBy], [Condition])
            VALUES
                (@VariantId, 'SHIPMENT', -@Quantity, @ReferenceDoc, @Notes, SYSUTCDATETIME(), @CreatedBy, 'NEW');";

        await conn.ExecuteAsync(txSql, new
        {
            VariantId = variantId,
            Quantity = quantity,
            ReferenceDoc = referenceDoc,
            Notes = notes,
            CreatedBy = userId
        }, transaction);
    }

    public async Task UpdateItemShippedQuantityAsync(long batchItemId, int shippedQty, string packStatus, IDbTransaction? transaction = null)
    {
        var conn = transaction?.Connection ?? connectionFactory.CreateConnection();

        const string sql = @"
            UPDATE [dbo].[PackingBatchItems]
            SET [ShippedQuantity] = [ShippedQuantity] + @ShippedQty,
                [PackStatus] = @PackStatus
            WHERE [Id] = @Id;";

        await conn.ExecuteAsync(sql, new { Id = batchItemId, ShippedQty = shippedQty, PackStatus = packStatus }, transaction);
    }

    public async Task CreatePendingResolutionAsync(ResolveBackorderDto dto, int? userId, IDbTransaction? transaction = null)
    {
        var conn = transaction?.Connection ?? connectionFactory.CreateConnection();

        const string itemSql = "SELECT [VariantId] FROM [dbo].[PackingBatchItems] WHERE [Id] = @Id;";
        int originalVariantId = await conn.ExecuteScalarAsync<int>(itemSql, new { Id = dto.BatchItemId }, transaction);

        const string resSql = @"
            INSERT INTO [dbo].[PackingPendingResolutions]
                ([BatchItemId], [OriginalVariantId], [ActualShippedVariantId], [ShippedQuantity], 
                 [ResolutionType], [CustomerAgreementNote], [FollowUpTrackingNo], [ResolvedAt], [ResolvedBy])
            VALUES
                (@BatchItemId, @OriginalVariantId, @ActualShippedVariantId, @ShippedQuantity, 
                 @ResolutionType, @CustomerAgreementNote, @FollowUpTrackingNo, SYSUTCDATETIME(), @ResolvedBy);";

        await conn.ExecuteAsync(resSql, new
        {
            dto.BatchItemId,
            OriginalVariantId = originalVariantId,
            dto.ActualShippedVariantId,
            dto.ShippedQuantity,
            dto.ResolutionType,
            dto.CustomerAgreementNote,
            dto.FollowUpTrackingNo,
            ResolvedBy = userId
        }, transaction);
    }

    public async Task<List<PackingBatchItemResponseDto>> GetPendingBackordersAsync(CancellationToken cancellationToken = default)
    {
        using var conn = connectionFactory.CreateConnection();

        const string sql = @"
            SELECT [Id], [BatchId], [Platform], [OrderNo], [TrackingNo], [ShippingProvider], 
                   [Sku], [VariantId], [ProductName], [OrderedQuantity], [ShippedQuantity], 
                   [OrderDate], [IsMultiParcel], [ParcelSeq], [TotalParcelsInOrder], 
                   [PackStatus], [IsSkuMatched]
            FROM [dbo].[PackingBatchItems]
            WHERE [OrderedQuantity] > [ShippedQuantity]
              AND [PackStatus] IN ('DRAFT', 'PARTIAL_SHIPPED')
            ORDER BY [OrderDate] DESC, [OrderNo];";

        return (await conn.QueryAsync<PackingBatchItemResponseDto>(sql)).ToList();
    }
}
