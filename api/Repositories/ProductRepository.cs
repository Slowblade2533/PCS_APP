using Dapper;
using PCS_API.DTOs;
using System.Data;

namespace PCS_API.Repositories;

public class ProductRepository(ISqlConnectionFactory connectionFactory) : IProductRepository
{
    public async Task<PagedResultDto<ProductListDto>> GetPagedProductsAsync(ProductSearchParamsDto search, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection();

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
                FROM dbo.Products p
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
                FROM dbo.Products p
                INNER JOIN dbo.Categories c ON p.CategoryId = c.CategoryId
                LEFT JOIN dbo.ProductVariants pv ON p.ProductId = pv.ProductId
                LEFT JOIN dbo.Stocks st ON pv.VariantId = st.VariantId
                LEFT JOIN dbo.ProductPrices pr ON pv.VariantId = pr.VariantId
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
                Items = items.ToList(),
                TotalCount = totalCount,
                PageNumber = search.PageNumber,
                PageSize = search.PageSize
            };
        }
        catch (OperationCanceledException)
        {
            return new PagedResultDto<ProductListDto>
            {
                Items = new List<ProductListDto>(),
                TotalCount = 0,
                PageNumber = search.PageNumber,
                PageSize = search.PageSize
            };
        }
    }

    public async Task<ProductDetailDto?> GetProductDetailAsync(int productId, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection();

        string sql = @"
                SELECT ProductId, ProductNameTh, ProductNameEn, Description, BrandName, CategoryId, ProductType, ProductStatus, IsStockTracked, InventoryGroup 
                FROM dbo.Products WHERE ProductId = @productId;

                SELECT pv.VariantId, pv.ProductId, pv.Sku, pv.Barcode, pv.VariantNameTh, pv.VariantNameEn, pv.Color, pv.SizeLabel, pv.StylePattern, pv.ImageUrl, pv.UnitOfMeasure, 
                       pv.Width, pv.Length, pv.Height, pv.Weight,
                       pr.BasePrice, pr.DiscountPrice, st.CurrentQuantity, st.ReorderPoint 
                FROM dbo.ProductVariants pv
                LEFT JOIN dbo.ProductPrices pr ON pv.VariantId = pr.VariantId
                LEFT JOIN dbo.Stocks st ON pv.VariantId = st.VariantId
                WHERE pv.ProductId = @productId;";

        using var multi = await connection.QueryMultipleAsync(new CommandDefinition(sql, new { productId }, cancellationToken: cancellationToken));
        var product = await multi.ReadFirstOrDefaultAsync<ProductDetailDto>();
        if (product == null) return null;

        var variants = (await multi.ReadAsync<ProductVariantDetailDto>()).ToList();
        product.Variants = variants;

        return product;
    }

    public async Task<int> InsertProductAsync(ProductCreateDto dto, IDbTransaction transaction, CancellationToken cancellationToken = default)
    {
        var conn = transaction.Connection ?? throw new InvalidOperationException("Transaction connection cannot be null.");
        string sql = @"
            INSERT INTO dbo.Products (ProductNameTh, ProductNameEn, Description, BrandName, CategoryId, ProductType, ProductStatus, CreatedBy, IsStockTracked, InventoryGroup)
            OUTPUT INSERTED.ProductId
            VALUES (@ProductNameTh, @ProductNameEn, @Description, @BrandName, @CategoryId, @ProductType, @ProductStatus, @CreatedBy, @IsStockTracked, @InventoryGroup);";
        return await conn.QuerySingleAsync<int>(new CommandDefinition(sql, dto, transaction: transaction, cancellationToken: cancellationToken));
    }

    public async Task<int> UpdateProductAsync(int productId, ProductCreateDto dto, IDbTransaction transaction, CancellationToken cancellationToken = default)
    {
        var conn = transaction.Connection ?? throw new InvalidOperationException("Transaction connection cannot be null.");
        string sql = @"
            UPDATE dbo.Products 
            SET ProductNameTh = @ProductNameTh, ProductNameEn = @ProductNameEn, 
                Description = @Description, BrandName = @BrandName, 
                CategoryId = @CategoryId,
                ProductType = @ProductType,
                ProductStatus = @ProductStatus,
                IsStockTracked = @IsStockTracked,
                InventoryGroup = @InventoryGroup
            WHERE ProductId = @ProductId;";
        
        var parameters = new DynamicParameters(dto);
        parameters.Add("ProductId", productId);
        return await conn.ExecuteAsync(new CommandDefinition(sql, parameters, transaction: transaction, cancellationToken: cancellationToken));
    }

    public async Task<int> InsertProductVariantAsync(int productId, VariantCreateDto v, IDbTransaction transaction, CancellationToken cancellationToken = default)
    {
        var conn = transaction.Connection ?? throw new InvalidOperationException("Transaction connection cannot be null.");
        var parameters = new DynamicParameters(v);
        parameters.Add("ProductId", productId);
        
        string sql = @"
            INSERT INTO dbo.ProductVariants (ProductId, Sku, Barcode, VariantNameTh, VariantNameEn, Color, SizeLabel, StylePattern, UnitOfMeasure, Width, Length, Height, Weight, ImageUrl)
            OUTPUT INSERTED.VariantId
            VALUES (@ProductId, @Sku, @Barcode, @VariantNameTh, @VariantNameEn, @Color, @SizeLabel, @StylePattern, @UnitOfMeasure, @Width, @Length, @Height, @Weight, @ImageUrl);";
        return await conn.QuerySingleAsync<int>(new CommandDefinition(sql, parameters, transaction: transaction, cancellationToken: cancellationToken));
    }

    public async Task UpdateProductVariantsAndPricesAsync(IEnumerable<object> updateDataList, IDbTransaction transaction, CancellationToken cancellationToken = default)
    {
        var conn = transaction.Connection ?? throw new InvalidOperationException("Transaction connection cannot be null.");
        string sql = @"
            UPDATE dbo.ProductVariants 
            SET Sku = @Sku, Barcode = @Barcode, VariantNameTh = @VariantNameTh, VariantNameEn = @VariantNameEn, Color = @Color, SizeLabel = @SizeLabel, StylePattern = @StylePattern, UnitOfMeasure = @UnitOfMeasure,
                Width = @Width, Length = @Length, Height = @Height, Weight = @Weight,
                ImageUrl = @ImageUrl
            WHERE VariantId = @UVariantId;
    
            UPDATE dbo.ProductPrices 
            SET BasePrice = @BasePrice, DiscountPrice = @DiscountPrice, UpdatedAt = GETDATE()
            WHERE VariantId = @UVariantId;

            UPDATE dbo.Stocks 
            SET CurrentQuantity = @CurrentQuantity, ReorderPoint = @ReorderPoint, UpdatedAt = GETDATE()
            WHERE VariantId = @UVariantId AND RowVersion = @URowVersion;";

        await conn.ExecuteAsync(new CommandDefinition(sql, updateDataList, transaction: transaction, cancellationToken: cancellationToken));
    }

    public async Task DeleteProductVariantsAsync(IEnumerable<int> variantIds, IDbTransaction transaction, CancellationToken cancellationToken = default)
    {
        var conn = transaction.Connection ?? throw new InvalidOperationException("Transaction connection cannot be null.");
        string sql = "DELETE FROM dbo.ProductVariants WHERE VariantId IN @Ids;";
        await conn.ExecuteAsync(new CommandDefinition(sql, new { Ids = variantIds }, transaction: transaction, cancellationToken: cancellationToken));
    }

    public async Task InsertProductPriceAsync(int variantId, decimal basePrice, decimal? discountPrice, IDbTransaction transaction, CancellationToken cancellationToken = default)
    {
        var conn = transaction.Connection ?? throw new InvalidOperationException("Transaction connection cannot be null.");
        string sql = @"
            INSERT INTO dbo.ProductPrices (VariantId, BasePrice, DiscountPrice, UpdatedAt)
            VALUES (@VariantId, @BasePrice, @DiscountPrice, GETDATE());";
        await conn.ExecuteAsync(new CommandDefinition(sql, new { VariantId = variantId, BasePrice = basePrice, DiscountPrice = discountPrice }, transaction: transaction, cancellationToken: cancellationToken));
    }

    public async Task<List<string>> CheckBarcodesExistAsync(IEnumerable<string> barcodes, IEnumerable<int>? ignoreVariantIds = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        var conn = transaction?.Connection ?? connectionFactory.CreateConnection();
        try
        {
            var parameters = new DynamicParameters();
            parameters.Add("Barcodes", barcodes);
            string sql = "SELECT Barcode FROM dbo.ProductVariants WHERE Barcode IN @Barcodes";
            
            if (ignoreVariantIds != null && ignoreVariantIds.Any())
            {
                sql += " AND VariantId NOT IN @IgnoreIds";
                parameters.Add("IgnoreIds", ignoreVariantIds);
            }

            var results = await conn.QueryAsync<string>(new CommandDefinition(sql, parameters, transaction: transaction, cancellationToken: cancellationToken));
            return results.ToList();
        }
        finally
        {
            if (transaction == null) conn.Dispose();
        }
    }

    public async Task<List<string>> CheckSkusExistAsync(IEnumerable<string> skus, IEnumerable<int>? ignoreVariantIds = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        var conn = transaction?.Connection ?? connectionFactory.CreateConnection();
        try
        {
            var parameters = new DynamicParameters();
            parameters.Add("Skus", skus);
            string sql = "SELECT Sku FROM dbo.ProductVariants WHERE Sku IN @Skus";
            
            if (ignoreVariantIds != null && ignoreVariantIds.Any())
            {
                sql += " AND VariantId NOT IN @IgnoreIds";
                parameters.Add("IgnoreIds", ignoreVariantIds);
            }

            var results = await conn.QueryAsync<string>(new CommandDefinition(sql, parameters, transaction: transaction, cancellationToken: cancellationToken));
            return results.ToList();
        }
        finally
        {
            if (transaction == null) conn.Dispose();
        }
    }

    public async Task<List<int>> GetExistingVariantIdsAsync(int productId, IDbTransaction transaction, CancellationToken cancellationToken = default)
    {
        var conn = transaction.Connection ?? throw new InvalidOperationException("Transaction connection cannot be null.");
        string sql = "SELECT VariantId FROM dbo.ProductVariants WHERE ProductId = @ProductId;";
        var results = await conn.QueryAsync<int>(new CommandDefinition(sql, new { ProductId = productId }, transaction: transaction, cancellationToken: cancellationToken));
        return results.ToList();
    }

    public async Task<List<(int VariantId, string? ImageUrl)>> GetVariantImagesAsync(IEnumerable<int> variantIds, IDbTransaction transaction, CancellationToken cancellationToken = default)
    {
        var conn = transaction.Connection ?? throw new InvalidOperationException("Transaction connection cannot be null.");
        string sql = "SELECT VariantId, ImageUrl FROM dbo.ProductVariants WHERE VariantId IN @Ids AND ImageUrl IS NOT NULL;";
        var results = await conn.QueryAsync<(int VariantId, string? ImageUrl)>(new CommandDefinition(sql, new { Ids = variantIds }, transaction: transaction, cancellationToken: cancellationToken));
        return results.ToList();
    }

    public async Task<List<(int VariantId, byte[] RowVersion)>> GetStockRowVersionsAsync(IEnumerable<int> variantIds, IDbTransaction transaction, CancellationToken cancellationToken = default)
    {
        var conn = transaction.Connection ?? throw new InvalidOperationException("Transaction connection cannot be null.");
        string sql = "SELECT VariantId, RowVersion FROM dbo.Stocks WHERE VariantId IN @Ids;";
        var results = await conn.QueryAsync<(int VariantId, byte[] RowVersion)>(new CommandDefinition(sql, new { Ids = variantIds }, transaction: transaction, cancellationToken: cancellationToken));
        return results.ToList();
    }
}