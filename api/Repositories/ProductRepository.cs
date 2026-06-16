using Dapper;
using Microsoft.Data.SqlClient;
using PCS_API.DTOs;
using PCS_API.Services;
using System.Text;

namespace PCS_API.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly ImageCleanupChannel _imageCleanup;
    public ProductRepository(ISqlConnectionFactory connectionFactory, ImageCleanupChannel imageCleanup)
    {
        _connectionFactory = connectionFactory;
        _imageCleanup = imageCleanup;
    }

    public async Task<ResultDto<int>> CreateProductWithVariantsAsync(ProductCreateDto dto)
    {
        using var connection = _connectionFactory.CreateConnection() as System.Data.Common.DbConnection;
        if (connection == null) throw new InvalidOperationException("Could not create DbConnection.");
        await connection.OpenAsync();
        using var transaction = await connection.BeginTransactionAsync();

        try
        {
            var duplicateSkusInPayload = dto.Variants
                .Where(v => !string.IsNullOrWhiteSpace(v.Sku))
                .GroupBy(v => v.Sku.Trim(), StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();
            if (duplicateSkusInPayload.Any())
            {
                return ResultDto<int>.Failure($"พบรหัส SKU ซ้ำกันเองภายในรายการที่คุณกรอกเข้ามา: '{string.Join(", ", duplicateSkusInPayload)}' กรุณาแก้ไขไม่ให้ซ้ำกัน");
            }

            var duplicateBarcodesInPayload = dto.Variants
                .Where(v => !string.IsNullOrWhiteSpace(v.Barcode))
                .GroupBy(v => v.Barcode!.Trim(), StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();
            if (duplicateBarcodesInPayload.Any())
            {
                return ResultDto<int>.Failure($"พบรหัสบาร์โค้ดซ้ำกันเองภายในรายการที่คุณกรอกเข้ามา: '{string.Join(", ", duplicateBarcodesInPayload)}' กรุณาแก้ไขไม่ให้ซ้ำกัน");
            }

            var barcodesToCheck = dto.Variants.Where(v => !string.IsNullOrWhiteSpace(v.Barcode)).Select(v => v.Barcode).ToList();
            if (barcodesToCheck.Any())
            {
                var existingBarcodes = await connection.QueryAsync<string>(
                    "SELECT Barcode FROM ProductVariants WHERE Barcode IN @Barcodes;", new { Barcodes = barcodesToCheck }, transaction);
                if (existingBarcodes.Any())
                {
                    return ResultDto<int>.Failure($"รหัสบาร์โค้ด '{string.Join(", ", existingBarcodes)}' มีอยู่ในระบบแล้ว ไม่สามารถใช้ซ้ำได้");
                }
            }

            var skusToCheck = dto.Variants.Where(v => !string.IsNullOrWhiteSpace(v.Sku)).Select(v => v.Sku).ToList();
            if (skusToCheck.Any())
            {
                var existingSkus = await connection.QueryAsync<string>(
                    "SELECT Sku FROM ProductVariants WHERE Sku IN @Skus;", new { Skus = skusToCheck }, transaction);
                if (existingSkus.Any())
                {
                    return ResultDto<int>.Failure($"รหัส SKU '{string.Join(", ", existingSkus)}' มีอยู่ในระบบแล้ว ไม่สามารถใช้ซ้ำได้");
                }
            }

            string productSql = @"
                    INSERT INTO Products (ProductNameTh, ProductNameEn, Description, BrandName, CategoryId, ProductType, ProductStatus, CreatedBy, IsStockTracked, InventoryGroup)
                    OUTPUT INSERTED.ProductId
                    VALUES (@ProductNameTh, @ProductNameEn, @Description, @BrandName, @CategoryId, @ProductType, @ProductStatus, @CreatedBy, @IsStockTracked, @InventoryGroup);";

            int productId = await connection.QuerySingleAsync<int>(productSql, dto, transaction);

            var batchSql = new StringBuilder();
            var batchParams = new DynamicParameters();
            batchParams.Add("ProductId", productId);
            batchParams.Add("CreatedBy", dto.CreatedBy);

            for (int i = 0; i < dto.Variants.Count; i++)
            {
                var v = dto.Variants[i];
                batchSql.AppendLine($"DECLARE @VId{i} INT;");
                batchSql.AppendLine($@"
                    INSERT INTO ProductVariants (ProductId, Sku, Barcode, VariantNameTh, VariantNameEn, Color, SizeLabel, StylePattern, UnitOfMeasure, Width, Length, Height, Weight, ImageUrl)
                    VALUES (@ProductId, @Sku{i}, @Barcode{i}, @VariantNameTh{i}, @VariantNameEn{i}, @Color{i}, @SizeLabel{i}, @StylePattern{i}, @UnitOfMeasure{i}, @Width{i}, @Length{i}, @Height{i}, @Weight{i}, @ImageUrl{i});
                    SET @VId{i} = SCOPE_IDENTITY();
                    INSERT INTO ProductPrices (VariantId, BasePrice, DiscountPrice, UpdatedAt)
                    VALUES (@VId{i}, @BasePrice{i}, @DiscountPrice{i}, GETDATE());
                    INSERT INTO Stocks (VariantId, CurrentQuantity, ReservedQuantity, ReorderPoint, UpdatedAt)
                    VALUES (@VId{i}, @CurrentQuantity{i}, 0, @ReorderPoint{i}, GETDATE());");

                if (dto.IsStockTracked && v.CurrentQuantity > 0)
                {
                    batchSql.AppendLine($@"
                    INSERT INTO StockTransactions (VariantId, TransactionType, Quantity, UnitCost, Notes, CreatedBy)
                    VALUES (@VId{i}, 'IN', @CurrentQuantity{i}, 0.00, N'บันทึกยอดตั้งต้นจากการเพิ่มสินค้าใหม่', @CreatedBy);");
                }

                batchParams.Add($"Sku{i}", v.Sku);
                batchParams.Add($"Barcode{i}", v.Barcode);
                batchParams.Add($"VariantNameTh{i}", v.VariantNameTh);
                batchParams.Add($"VariantNameEn{i}", v.VariantNameEn);
                batchParams.Add($"Color{i}", v.Color);
                batchParams.Add($"SizeLabel{i}", v.SizeLabel);
                batchParams.Add($"StylePattern{i}", v.StylePattern);
                batchParams.Add($"UnitOfMeasure{i}", v.UnitOfMeasure);
                batchParams.Add($"Width{i}", v.Width);
                batchParams.Add($"Length{i}", v.Length);
                batchParams.Add($"Height{i}", v.Height);
                batchParams.Add($"Weight{i}", v.Weight);
                batchParams.Add($"ImageUrl{i}", v.ImageUrl);
                batchParams.Add($"BasePrice{i}", v.BasePrice);
                batchParams.Add($"DiscountPrice{i}", v.DiscountPrice);
                batchParams.Add($"CurrentQuantity{i}", v.CurrentQuantity);
                batchParams.Add($"ReorderPoint{i}", v.ReorderPoint);
            }

            if (batchSql.Length > 0)
            {
                await connection.ExecuteAsync(batchSql.ToString(), batchParams, transaction);
            }

            await transaction.CommitAsync();
            return ResultDto<int>.Success(productId);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<PagedResultDto<ProductListDto>> GetPagedProductsAsync(ProductSearchParamsDto search, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        var parameters = new DynamicParameters();
        parameters.Add("SearchTerm", string.IsNullOrEmpty(search.SearchTerm) ? null : $"%{search.SearchTerm}%");
        parameters.Add("CategoryId", search.CategoryId);
        parameters.Add("ProductStatus", string.IsNullOrEmpty(search.ProductStatus) ? null : search.ProductStatus);
        parameters.Add("ProductType", string.IsNullOrEmpty(search.ProductType) ? null : search.ProductType);
        parameters.Add("InventoryGroup", string.IsNullOrEmpty(search.InventoryGroup) ? null : search.InventoryGroup);
        parameters.Add("PageSize", search.PageSize);
        parameters.Add("Offset", search.GetSafeOffset());

        string sql = @"
                SELECT COUNT(*)
                FROM Products p
                WHERE 
                    (@SearchTerm IS NULL OR p.ProductNameTh LIKE @SearchTerm OR p.ProductNameEn LIKE @SearchTerm OR p.BrandName LIKE @SearchTerm)
                    AND (@CategoryId IS NULL OR p.CategoryId = @CategoryId)
                    AND (@ProductStatus IS NULL OR p.ProductStatus = @ProductStatus)
                    AND (@ProductType IS NULL OR p.ProductType = @ProductType)
                    AND (@InventoryGroup IS NULL OR p.InventoryGroup = @InventoryGroup);

                SELECT 
                    p.ProductId,
                    p.ProductNameTh,
                    p.ProductNameEn,
                    p.BrandName,
                    c.CategoryName,
                    p.ProductType,
                    p.ProductStatus,
                    p.IsStockTracked,
                    p.InventoryGroup,
                    COUNT(pv.VariantId) AS TotalVariants,
                    ISNULL(SUM(st.AvailableQuantity), 0) AS TotalAvailableStock,
                    ISNULL(MIN(pr.BasePrice), 0) AS MinPrice,
                    MAX(pv.ImageUrl) AS ImageUrl
                FROM Products p
                INNER JOIN Categories c ON p.CategoryId = c.CategoryId
                LEFT JOIN ProductVariants pv ON p.ProductId = pv.ProductId
                LEFT JOIN Stocks st ON pv.VariantId = st.VariantId
                LEFT JOIN ProductPrices pr ON pv.VariantId = pr.VariantId
                WHERE 
                    (@SearchTerm IS NULL OR p.ProductNameTh LIKE @SearchTerm OR p.ProductNameEn LIKE @SearchTerm OR p.BrandName LIKE @SearchTerm)
                    AND (@CategoryId IS NULL OR p.CategoryId = @CategoryId)
                    AND (@ProductStatus IS NULL OR p.ProductStatus = @ProductStatus)
                    AND (@ProductType IS NULL OR p.ProductType = @ProductType)
                    AND (@InventoryGroup IS NULL OR p.InventoryGroup = @InventoryGroup)
                GROUP BY 
                    p.ProductId, p.ProductNameTh, p.ProductNameEn, p.BrandName, c.CategoryName, p.ProductType, p.ProductStatus, p.IsStockTracked, p.InventoryGroup
                ORDER BY p.ProductId DESC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

        var command = new CommandDefinition(sql, parameters, cancellationToken: cancellationToken);
        
        try
        {
            using var multi = await connection.QueryMultipleAsync(command);
            int totalCount = await multi.ReadSingleAsync<int>();
            var items = await multi.ReadAsync<ProductListDto>();

            return new PagedResultDto<ProductListDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = search.PageNumber,
                PageSize = search.PageSize
            };
        }
        catch (OperationCanceledException)
        {
            // Handle cases where the client cancels the request (e.g. typing in search box too fast)
            // This prevents unhandled TaskCanceledException errors in the application.
            return new PagedResultDto<ProductListDto>
            {
                Items = new List<ProductListDto>(),
                TotalCount = 0,
                PageNumber = search.PageNumber,
                PageSize = search.PageSize
            };
        }
    }

    public async Task<ProductDetailDto?> GetProductDetailAsync(int productId)
    {
        using var connection = _connectionFactory.CreateConnection();

        string sql = @"
                SELECT ProductId, ProductNameTh, ProductNameEn, Description, BrandName, CategoryId, ProductType, ProductStatus, IsStockTracked, InventoryGroup 
                FROM Products WHERE ProductId = @productId;

                SELECT pv.VariantId, pv.ProductId, pv.Sku, pv.Barcode, pv.VariantNameTh, pv.VariantNameEn, pv.Color, pv.SizeLabel, pv.StylePattern, pv.ImageUrl, pv.UnitOfMeasure, 
                       pv.Width, pv.Length, pv.Height, pv.Weight,
                       pr.BasePrice, pr.DiscountPrice, st.CurrentQuantity, st.ReorderPoint 
                FROM ProductVariants pv
                LEFT JOIN ProductPrices pr ON pv.VariantId = pr.VariantId
                LEFT JOIN Stocks st ON pv.VariantId = st.VariantId
                WHERE pv.ProductId = @productId;";

        using var multi = await connection.QueryMultipleAsync(sql, new { productId });
        var product = await multi.ReadFirstOrDefaultAsync<ProductDetailDto>();
        if (product == null) return null;

        var variants = (await multi.ReadAsync<ProductVariantDetailDto>()).ToList();
        product.Variants = variants;

        return product;
    }

    public async Task<ResultDto<bool>> UpdateProductWithVariantsAsync(int productId, ProductCreateDto dto)
    {
        using var connection = _connectionFactory.CreateConnection() as System.Data.Common.DbConnection;
        if (connection == null) throw new InvalidOperationException("Could not create DbConnection.");
        await connection.OpenAsync();
        using var transaction = await connection.BeginTransactionAsync();

        try
        {
            var duplicateSkusInPayload = dto.Variants
                .Where(v => !string.IsNullOrWhiteSpace(v.Sku))
                .GroupBy(v => v.Sku.Trim(), StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();
            if (duplicateSkusInPayload.Any())
            {
                return ResultDto<bool>.Failure($"พบรหัส SKU ซ้ำกันเองภายในรายการที่คุณกรอกเข้ามา: '{string.Join(", ", duplicateSkusInPayload)}' กรุณาแก้ไขไม่ให้ซ้ำกัน");
            }

            var duplicateBarcodesInPayload = dto.Variants
                .Where(v => !string.IsNullOrWhiteSpace(v.Barcode))
                .GroupBy(v => v.Barcode!.Trim(), StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();
            if (duplicateBarcodesInPayload.Any())
            {
                return ResultDto<bool>.Failure($"พบรหัสบาร์โค้ดซ้ำกันเองภายในรายการที่คุณกรอกเข้ามา: '{string.Join(", ", duplicateBarcodesInPayload)}' กรุณาแก้ไขไม่ให้ซ้ำกัน");
            }
            
            var filesToCheckForDeletion = new List<string>();

            var incomingVariantIds = dto.Variants
                .Where(x => x.VariantId.HasValue && x.VariantId.Value > 0)
                .Select(x => x.VariantId!.Value)
                .ToList();

            string getCurrentVariantsSql = "SELECT VariantId FROM ProductVariants WHERE ProductId = @ProductId;";
            var existingVariantIds = (await connection.QueryAsync<int>(getCurrentVariantsSql, new { ProductId = productId }, transaction)).ToList();
            var variantsToDelete = existingVariantIds.Except(incomingVariantIds).ToList();

            if (variantsToDelete.Any())
            {
                var imagesOfDeletedVariants = await connection.QueryAsync<string>(
                    "SELECT ImageUrl FROM ProductVariants WHERE VariantId IN @Ids AND ImageUrl IS NOT NULL;",
                    new { Ids = variantsToDelete }, transaction);
                filesToCheckForDeletion.AddRange(imagesOfDeletedVariants);

                await connection.ExecuteAsync("DELETE FROM ProductVariants WHERE VariantId IN @Ids;", new { Ids = variantsToDelete }, transaction);
            }

            var existingVariantsToUpdate = dto.Variants.Where(v => v.VariantId != null && v.VariantId > 0).Select(v => v.VariantId).ToList();
            if (!existingVariantsToUpdate.Any())
            {
                existingVariantsToUpdate.Add(-1);
            }

            var barcodes = dto.Variants.Where(v => !string.IsNullOrWhiteSpace(v.Barcode)).Select(v => v.Barcode).ToList();
            if (barcodes.Any())
            {
                string checkBarcodeSql = @"SELECT Barcode FROM ProductVariants WHERE Barcode IN @Barcodes AND VariantId NOT IN @IgnoreIds;";
                var existingBarcodes = await connection.QueryAsync<string>(checkBarcodeSql, new { Barcodes = barcodes, IgnoreIds = existingVariantsToUpdate }, transaction);
                if (existingBarcodes.Any())
                {
                    return ResultDto<bool>.Failure($"รหัสบาร์โค้ด '{string.Join(", ", existingBarcodes)}' มีอยู่ในระบบแล้ว ไม่สามารถใช้ซ้ำได้");
                }
            }

            var skus = dto.Variants.Where(v => !string.IsNullOrWhiteSpace(v.Sku)).Select(v => v.Sku).ToList();
            if (skus.Any())
            {
                string checkSkuSql = @"SELECT Sku FROM ProductVariants WHERE Sku IN @Skus AND VariantId NOT IN @IgnoreIds;";
                var existingSkus = await connection.QueryAsync<string>(checkSkuSql, new { Skus = skus, IgnoreIds = existingVariantsToUpdate }, transaction);
                if (existingSkus.Any())
                {
                    return ResultDto<bool>.Failure($"รหัส SKU '{string.Join(", ", existingSkus)}' มีอยู่ในระบบแล้ว ไม่สามารถใช้ซ้ำได้");
                } 
            }

            string updateProductSql = @"
                    UPDATE Products 
                    SET ProductNameTh = @ProductNameTh, ProductNameEn = @ProductNameEn, 
                        Description = @Description, BrandName = @BrandName, 
                        CategoryId = @CategoryId,
                        ProductType = @ProductType,
                        ProductStatus = @ProductStatus,
                        IsStockTracked = @IsStockTracked,
                        InventoryGroup = @InventoryGroup
                    WHERE ProductId = @ProductId;";

            var productParams = new DynamicParameters(dto);
            productParams.Add("ProductId", productId);
            int rowsAffected = await connection.ExecuteAsync(updateProductSql, productParams, transaction);
            if (rowsAffected == 0)
            {
                return ResultDto<bool>.Failure("ไม่พบสินค้าที่ต้องการแก้ไข");
            }

            var existingVarIdsForUpdate = dto.Variants
                .Where(v => v.VariantId != null && v.VariantId > 0)
                .Select(v => v.VariantId!.Value)
                .ToList();

            Dictionary<int, string?> oldImageMap = new();
            if (existingVarIdsForUpdate.Any())
            {
                var oldImages = await connection.QueryAsync<(int VariantId, string? ImageUrl)>(
                    "SELECT VariantId, ImageUrl FROM ProductVariants WHERE VariantId IN @Ids;",
                    new { Ids = existingVarIdsForUpdate }, transaction);
                oldImageMap = oldImages.ToDictionary(x => x.VariantId, x => x.ImageUrl);
            }

            Dictionary<int, byte[]> rowVersionMap = new();
            if (existingVarIdsForUpdate.Any())
            {
                var stockSnapshots = await connection.QueryAsync<(int VariantId, byte[] RowVersion)>(
                    "SELECT VariantId, RowVersion FROM Stocks WHERE VariantId IN @Ids;",
                    new { Ids = existingVarIdsForUpdate }, transaction);
                rowVersionMap = stockSnapshots.ToDictionary(x => x.VariantId, x => x.RowVersion);
            }

            var newVariants = dto.Variants.Where(v => v.VariantId == null || v.VariantId == 0).ToList();
            if (newVariants.Any())
            {
                var batchSql = new StringBuilder();
                var batchParams = new DynamicParameters();
                batchParams.Add("ProductId", productId);
                batchParams.Add("CreatedBy", dto.UpdatedBy ?? dto.CreatedBy);

                for (int i = 0; i < newVariants.Count; i++)
                {
                    var v = newVariants[i];
                    batchSql.AppendLine($"DECLARE @NewVId{i} INT;");
                    batchSql.AppendLine($@"
                        INSERT INTO ProductVariants (ProductId, Sku, Barcode, VariantNameTh, VariantNameEn, Color, SizeLabel, StylePattern, UnitOfMeasure, Width, Length, Height, Weight, ImageUrl)
                        VALUES (@ProductId, @NSku{i}, @NBarcode{i}, @NVariantNameTh{i}, @NVariantNameEn{i}, @NColor{i}, @NSizeLabel{i}, @NStylePattern{i}, @NUnitOfMeasure{i}, @NWidth{i}, @NLength{i}, @NHeight{i}, @NWeight{i}, @NImageUrl{i});
                        SET @NewVId{i} = SCOPE_IDENTITY();
                        INSERT INTO ProductPrices (VariantId, BasePrice, DiscountPrice, UpdatedAt)
                        VALUES (@NewVId{i}, @NBasePrice{i}, @NDiscountPrice{i}, GETDATE());
                        INSERT INTO Stocks (VariantId, CurrentQuantity, ReservedQuantity, ReorderPoint, UpdatedAt)
                        VALUES (@NewVId{i}, @NCurrentQuantity{i}, 0, @NReorderPoint{i}, GETDATE());");

                    if (dto.IsStockTracked && v.CurrentQuantity > 0)
                    {
                        batchSql.AppendLine($@"
                        INSERT INTO StockTransactions (VariantId, TransactionType, Quantity, UnitCost, Notes, CreatedBy)
                        VALUES (@NewVId{i}, 'IN', @NCurrentQuantity{i}, 0.00, N'บันทึกยอดตั้งต้นจากการเพิ่ม SKU ใหม่ในโหมดแก้ไข', @CreatedBy);");
                    }

                    batchParams.Add($"NSku{i}", v.Sku);
                    batchParams.Add($"NBarcode{i}", v.Barcode);
                    batchParams.Add($"NVariantNameTh{i}", v.VariantNameTh);
                    batchParams.Add($"NVariantNameEn{i}", v.VariantNameEn);
                    batchParams.Add($"NColor{i}", v.Color);
                    batchParams.Add($"NSizeLabel{i}", v.SizeLabel);
                    batchParams.Add($"NStylePattern{i}", v.StylePattern);
                    batchParams.Add($"NUnitOfMeasure{i}", v.UnitOfMeasure);
                    batchParams.Add($"NWidth{i}", v.Width);
                    batchParams.Add($"NLength{i}", v.Length);
                    batchParams.Add($"NHeight{i}", v.Height);
                    batchParams.Add($"NWeight{i}", v.Weight);
                    batchParams.Add($"NImageUrl{i}", v.ImageUrl);
                    batchParams.Add($"NBasePrice{i}", v.BasePrice);
                    batchParams.Add($"NDiscountPrice{i}", v.DiscountPrice);
                    batchParams.Add($"NCurrentQuantity{i}", v.CurrentQuantity);
                    batchParams.Add($"NReorderPoint{i}", v.ReorderPoint);
                }

                await connection.ExecuteAsync(batchSql.ToString(), batchParams, transaction);
            }

            var existingVariants = dto.Variants.Where(v => v.VariantId != null && v.VariantId > 0).ToList();
            if (existingVariants.Any())
            {
                var updateSql = new StringBuilder();
                var updateParams = new DynamicParameters();

                for (int i = 0; i < existingVariants.Count; i++)
                {
                    var v = existingVariants[i];
                    int variantId = v.VariantId!.Value;

                    if (oldImageMap.TryGetValue(variantId, out var oldImageUrl))
                    {
                        if (!string.Equals(oldImageUrl, v.ImageUrl, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(oldImageUrl))
                        {
                            filesToCheckForDeletion.Add(oldImageUrl);
                        }
                    }

                    updateSql.AppendLine($@"
                        UPDATE ProductVariants 
                        SET Sku = @USku{i}, Barcode = @UBarcode{i}, VariantNameTh = @UVariantNameTh{i}, VariantNameEn = @UVariantNameEn{i}, Color = @UColor{i}, SizeLabel = @USizeLabel{i}, StylePattern = @UStylePattern{i}, UnitOfMeasure = @UUnitOfMeasure{i},
                            Width = @UWidth{i}, Length = @ULength{i}, Height = @UHeight{i}, Weight = @UWeight{i},
                            ImageUrl = @UImageUrl{i}
                        WHERE VariantId = @UVariantId{i};
                
                        UPDATE ProductPrices 
                        SET BasePrice = @UBasePrice{i}, DiscountPrice = @UDiscountPrice{i}, UpdatedAt = GETDATE()
                        WHERE VariantId = @UVariantId{i};

                        UPDATE Stocks 
                        SET CurrentQuantity = @UCurrentQuantity{i}, ReorderPoint = @UReorderPoint{i}, UpdatedAt = GETDATE()
                        WHERE VariantId = @UVariantId{i} AND RowVersion = @URowVersion{i};");

                    updateParams.Add($"UVariantId{i}", variantId);
                    updateParams.Add($"USku{i}", v.Sku);
                    updateParams.Add($"UBarcode{i}", v.Barcode);
                    updateParams.Add($"UVariantNameTh{i}", v.VariantNameTh);
                    updateParams.Add($"UVariantNameEn{i}", v.VariantNameEn);
                    updateParams.Add($"UColor{i}", v.Color);
                    updateParams.Add($"USizeLabel{i}", v.SizeLabel);
                    updateParams.Add($"UStylePattern{i}", v.StylePattern);
                    updateParams.Add($"UUnitOfMeasure{i}", v.UnitOfMeasure);
                    updateParams.Add($"UWidth{i}", v.Width);
                    updateParams.Add($"ULength{i}", v.Length);
                    updateParams.Add($"UHeight{i}", v.Height);
                    updateParams.Add($"UWeight{i}", v.Weight);
                    updateParams.Add($"UImageUrl{i}", v.ImageUrl);
                    updateParams.Add($"UBasePrice{i}", v.BasePrice);
                    updateParams.Add($"UDiscountPrice{i}", v.DiscountPrice);
                    updateParams.Add($"UCurrentQuantity{i}", v.CurrentQuantity);
                    updateParams.Add($"UReorderPoint{i}", v.ReorderPoint);
                    updateParams.Add($"URowVersion{i}", rowVersionMap.GetValueOrDefault(variantId, Array.Empty<byte>()));
                }

                await connection.ExecuteAsync(updateSql.ToString(), updateParams, transaction);
            }

            await transaction.CommitAsync();

            if (filesToCheckForDeletion.Any())
            {
                _imageCleanup.Writer.TryWrite(filesToCheckForDeletion.Distinct().ToList());
            }

            return ResultDto<bool>.Success(true);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}