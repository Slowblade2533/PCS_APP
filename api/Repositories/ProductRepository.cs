using Dapper;
using Microsoft.Data.SqlClient;
using PCS_API.DTOs;

namespace PCS_API.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly string _connectionString;

    public ProductRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new ArgumentNullException(nameof(configuration));
    }

    public async Task<int> CreateProductWithVariantsAsync(ProductCreateDto dto)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        using var transaction = await connection.BeginTransactionAsync();

        try
        {
            // 1. In-Memory Duplicate Check: ตรวจสอบการกรอกข้อมูลซ้ำกันเองภายในฟอร์ม
            var duplicateSkusInPayload = dto.Variants
                .Where(v => !string.IsNullOrWhiteSpace(v.Sku))
                .GroupBy(v => v.Sku.Trim(), StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicateSkusInPayload.Any())
                throw new InvalidOperationException($"พบรหัส SKU ซ้ำกันเองภายในรายการที่คุณกรอกเข้ามา: '{string.Join(", ", duplicateSkusInPayload)}' กรุณาแก้ไขไม่ให้ซ้ำกัน");

            var duplicateBarcodesInPayload = dto.Variants
                .Where(v => !string.IsNullOrWhiteSpace(v.Barcode))
                .GroupBy(v => v.Barcode.Trim(), StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicateBarcodesInPayload.Any())
                throw new InvalidOperationException($"พบรหัสบาร์โค้ดซ้ำกันเองภายในรายการที่คุณกรอกเข้ามา: '{string.Join(", ", duplicateBarcodesInPayload)}' กรุณาแก้ไขไม่ให้ซ้ำกัน");

            // 2. Database Check: ตรวจสอบความซ้ำซ้อนกับสินค้าตัวอื่นในฐานข้อมูลก่อนบันทึก
            foreach (var v in dto.Variants)
            {
                if (!string.IsNullOrWhiteSpace(v.Barcode))
                {
                    int barcodeCount = await connection.ExecuteScalarAsync<int>(
                        "SELECT COUNT(1) FROM ProductVariants WHERE Barcode = @Barcode;", new { v.Barcode }, transaction);
                    if (barcodeCount > 0)
                        throw new InvalidOperationException($"รหัสบาร์โค้ด '{v.Barcode}' มีอยู่ในระบบแล้ว ไม่สามารถใช้ซ้ำได้");
                }

                if (!string.IsNullOrWhiteSpace(v.Sku))
                {
                    int skuCount = await connection.ExecuteScalarAsync<int>(
                        "SELECT COUNT(1) FROM ProductVariants WHERE Sku = @Sku;", new { v.Sku }, transaction);
                    if (skuCount > 0)
                        throw new InvalidOperationException($"รหัส SKU '{v.Sku}' มีอยู่ในระบบแล้ว ไม่สามารถใช้ซ้ำได้");
                }
            }

            string productSql = @"
                    INSERT INTO Products (ProductNameTh, ProductNameEn, Description, BrandName, CategoryId, ProductType, ProductStatus, CreatedBy)
                    OUTPUT INSERTED.ProductId
                    VALUES (@ProductNameTh, @ProductNameEn, @Description, @BrandName, @CategoryId, @ProductType, @ProductStatus, @CreatedBy);";

            int productId = await connection.QuerySingleAsync<int>(productSql, dto, transaction);

            foreach (var v in dto.Variants)
            {
                string variantSql = @"
                        INSERT INTO ProductVariants (ProductId, Sku, Barcode, UnitOfMeasure, Width, Length, Height, Weight, ImageUrl)
                        OUTPUT INSERTED.VariantId
                        VALUES (@ProductId, @Sku, @Barcode, @UnitOfMeasure, @Width, @Length, @Height, @Weight, @ImageUrl);";

                var variantParams = new DynamicParameters(v);
                variantParams.Add("ProductId", productId);

                int variantId = await connection.QuerySingleAsync<int>(variantSql, variantParams, transaction);

                string priceSql = @"
                        INSERT INTO ProductPrices (VariantId, BasePrice, DiscountPrice, UpdatedAt)
                        VALUES (@VariantId, @BasePrice, @DiscountPrice, GETDATE());";

                await connection.ExecuteAsync(priceSql, new { VariantId = variantId, v.BasePrice, v.DiscountPrice }, transaction);

                string stockSql = @"
                        INSERT INTO Stocks (VariantId, CurrentQuantity, ReservedQuantity, ReorderPoint, UpdatedAt)
                        VALUES (@VariantId, @CurrentQuantity, 0, @ReorderPoint, GETDATE());";

                await connection.ExecuteAsync(stockSql, new { VariantId = variantId, v.CurrentQuantity, v.ReorderPoint }, transaction);

                if (v.CurrentQuantity > 0)
                {
                    string transactionSql = @"
                            INSERT INTO StockTransactions (VariantId, TransactionType, Quantity, UnitCost, Notes, CreatedBy)
                            VALUES (@VariantId, 'IN', @Quantity, @UnitCost, N'บันทึกยอดตั้งต้นจากการเพิ่มสินค้าใหม่', @CreatedBy);";

                    await connection.ExecuteAsync(transactionSql, new
                    {
                        VariantId = variantId,
                        Quantity = v.CurrentQuantity,
                        UnitCost = 0.00m,
                        CreatedBy = dto.CreatedBy
                    }, transaction);
                }
            }

            await transaction.CommitAsync();
            return productId;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<PagedResult<ProductListDto>> GetPagedProductsAsync(ProductSearchParams search)
    {
        using var connection = new SqlConnection(_connectionString);

        string sql = @"
                SELECT 
                    p.ProductId,
                    p.ProductNameTh,
                    p.ProductNameEn,
                    p.BrandName,
                    c.CategoryName,
                    p.ProductType,
                    p.ProductStatus,
                    COUNT(pv.VariantId) AS TotalVariants,
                    ISNULL(SUM(st.AvailableQuantity), 0) AS TotalAvailableStock,
                    ISNULL(MIN(pr.BasePrice), 0) AS MinPrice,
                    COUNT(*) OVER() AS TotalCount
                FROM Products p
                INNER JOIN Categories c ON p.CategoryId = c.CategoryId
                LEFT JOIN ProductVariants pv ON p.ProductId = pv.ProductId
                LEFT JOIN Stocks st ON pv.VariantId = st.VariantId
                LEFT JOIN ProductPrices pr ON pv.VariantId = pr.VariantId
                WHERE 
                    (@SearchTerm IS NULL OR p.ProductNameTh LIKE @SearchTerm OR p.ProductNameEn LIKE @SearchTerm OR p.BrandName LIKE @SearchTerm)
                    AND (@CategoryId IS NULL OR p.CategoryId = @CategoryId)
                    AND (@ProductStatus IS NULL OR p.ProductStatus = @ProductStatus)
                GROUP BY 
                    p.ProductId, p.ProductNameTh, p.ProductNameEn, p.BrandName, c.CategoryName, p.ProductType, p.ProductStatus
                ORDER BY p.ProductId DESC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

        var parameters = new DynamicParameters();
        parameters.Add("SearchTerm", string.IsNullOrEmpty(search.SearchTerm) ? null : $"%{search.SearchTerm}%");
        parameters.Add("CategoryId", search.CategoryId);
        parameters.Add("ProductStatus", string.IsNullOrEmpty(search.ProductStatus) ? null : search.ProductStatus);
        parameters.Add("PageSize", search.PageSize);
        parameters.Add("Offset", (search.PageNumber - 1) * search.PageSize);

        var items = await connection.QueryAsync<ProductListDto>(sql, parameters);
        int totalCount = items.FirstOrDefault()?.TotalCount ?? 0;

        return new PagedResult<ProductListDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = search.PageNumber,
            PageSize = search.PageSize
        };
    }

    public async Task<object?> GetProductDetailAsync(int productId)
    {
        using var connection = new SqlConnection(_connectionString);

        string sql = @"
                SELECT * FROM Products WHERE ProductId = @productId;
                SELECT pv.*, pr.BasePrice, pr.DiscountPrice, st.CurrentQuantity, st.ReorderPoint 
                FROM ProductVariants pv
                LEFT JOIN ProductPrices pr ON pv.VariantId = pr.VariantId
                LEFT JOIN Stocks st ON pv.VariantId = st.VariantId
                WHERE pv.ProductId = @productId;";

        using var multi = await connection.QueryMultipleAsync(sql, new { productId });
        var product = await multi.ReadFirstOrDefaultAsync<dynamic>();
        if (product == null) return null;

        var variants = (await multi.ReadAsync<dynamic>()).ToList();
        product.variants = variants;

        return product;
    }

    public async Task<bool> UpdateProductWithVariantsAsync(int productId, ProductCreateDto dto)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        using var transaction = await connection.BeginTransactionAsync();

        try
        {
            // 1. In-Memory Duplicate Check: สกัดกั้นการกรอกข้อมูลซ้ำกันเองภายในฟอร์มหน้าเว็บทันที
            var duplicateSkusInPayload = dto.Variants
                .Where(v => !string.IsNullOrWhiteSpace(v.Sku))
                .GroupBy(v => v.Sku.Trim(), StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicateSkusInPayload.Any())
                throw new InvalidOperationException($"พบรหัส SKU ซ้ำกันเองภายในรายการที่คุณกรอกเข้ามา: '{string.Join(", ", duplicateSkusInPayload)}' กรุณาแก้ไขไม่ให้ซ้ำกัน");

            var duplicateBarcodesInPayload = dto.Variants
                .Where(v => !string.IsNullOrWhiteSpace(v.Barcode))
                .GroupBy(v => v.Barcode.Trim(), StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicateBarcodesInPayload.Any())
                throw new InvalidOperationException($"พบรหัสบาร์โค้ดซ้ำกันเองภายในรายการที่คุณกรอกเข้ามา: '{string.Join(", ", duplicateBarcodesInPayload)}' กรุณาแก้ไขไม่ให้ซ้ำกัน");

            // 2. จัดเตรียมลิสต์ชี้เป้าภาพขยะที่จะคัดออกหลังบันทึก
            var filesToCheckForDeletion = new List<string>();

            // 3. ตรวจสอบหากมี SKU ย่อยตัวไหนถูกผู้ใช้งานกด 'ลบ' ออกไปจากรายการ
            var incomingVariantIds = dto.Variants
                .Where(x => x.VariantId.HasValue && x.VariantId.Value > 0)
                .Select(x => x.VariantId!.Value)
                .ToList();

            string getCurrentVariantsSql = "SELECT VariantId FROM ProductVariants WHERE ProductId = @ProductId;";
            var existingVariantIds = (await connection.QueryAsync<int>(getCurrentVariantsSql, new { ProductId = productId }, transaction)).ToList();
            var variantsToDelete = existingVariantIds.Except(incomingVariantIds).ToList();

            if (variantsToDelete.Any())
            {
                // ดึง Path รูปของตัวที่กำลังจะโดนลบไปเก็บในถังตรวจสอบขยะ
                var imagesOfDeletedVariants = await connection.QueryAsync<string>(
                    "SELECT ImageUrl FROM ProductVariants WHERE VariantId IN @Ids AND ImageUrl IS NOT NULL;",
                    new { Ids = variantsToDelete }, transaction);
                filesToCheckForDeletion.AddRange(imagesOfDeletedVariants);

                // สั่งลบบรรทัดเดียว (ระบบจะ CASCADE DELETE ไปที่ตารางราคา สต็อก และทรานแซกชันให้เองอัตโนมัติ)
                await connection.ExecuteAsync("DELETE FROM ProductVariants WHERE VariantId IN @Ids;", new { Ids = variantsToDelete }, transaction);
            }

            // 4. Database Check: ตรวจสอบความซ้ำซ้อนกับสินค้าชิ้นอื่นๆ ในระบบดิบ
            foreach (var v in dto.Variants)
            {
                if (!string.IsNullOrWhiteSpace(v.Barcode))
                {
                    string checkBarcodeSql = (v.VariantId == null || v.VariantId == 0)
                        ? "SELECT COUNT(1) FROM ProductVariants WHERE Barcode = @Barcode;"
                        : "SELECT COUNT(1) FROM ProductVariants WHERE Barcode = @Barcode AND VariantId <> @VariantId;";

                    int barcodeCount = await connection.ExecuteScalarAsync<int>(checkBarcodeSql, new { v.Barcode, v.VariantId }, transaction);
                    if (barcodeCount > 0)
                        throw new InvalidOperationException($"รหัสบาร์โค้ด '{v.Barcode}' มีอยู่ในระบบแล้ว ไม่สามารถใช้ซ้ำได้");
                }

                if (!string.IsNullOrWhiteSpace(v.Sku))
                {
                    string checkSkuSql = (v.VariantId == null || v.VariantId == 0)
                        ? "SELECT COUNT(1) FROM ProductVariants WHERE Sku = @Sku;"
                        : "SELECT COUNT(1) FROM ProductVariants WHERE Sku = @Sku AND VariantId <> @VariantId;";

                    int skuCount = await connection.ExecuteScalarAsync<int>(checkSkuSql, new { v.Sku, v.VariantId }, transaction);
                    if (skuCount > 0)
                        throw new InvalidOperationException($"รหัส SKU '{v.Sku}' มีอยู่ในระบบแล้ว ไม่สามารถใช้ซ้ำได้");
                }
            }

            // 5. บันทึกปรับปรุงข้อมูลสินค้าหลัก (Products Table)
            string updateProductSql = @"
                    UPDATE Products 
                    SET ProductNameTh = @ProductNameTh, ProductNameEn = @ProductNameEn, 
                        Description = @Description, BrandName = @BrandName, 
                        CategoryId = @CategoryId, ProductType = @ProductType, 
                        ProductStatus = @ProductStatus
                    WHERE ProductId = @ProductId;";

            var productParams = new DynamicParameters(dto);
            productParams.Add("ProductId", productId);
            int rowsAffected = await connection.ExecuteAsync(updateProductSql, productParams, transaction);
            if (rowsAffected == 0) return false;

            // 6. ลูปเพื่อเพิ่มหรือแก้ไขรายการ SKU ย่อยรายตัว
            foreach (var v in dto.Variants)
            {
                if (v.VariantId == null || v.VariantId == 0)
                {
                    // กรณีเพิ่ม SKU ใหม่เข้าไปในสินค้ารายการเดิม
                    string insertVariantSql = @"
                            INSERT INTO ProductVariants (ProductId, Sku, Barcode, UnitOfMeasure, Width, Length, Height, Weight, ImageUrl)
                            OUTPUT INSERTED.VariantId
                            VALUES (@ProductId, @Sku, @Barcode, @UnitOfMeasure, @Width, @Length, @Height, @Weight, @ImageUrl);";

                    var variantParams = new DynamicParameters(v);
                    variantParams.Add("ProductId", productId);

                    int newVariantId = await connection.QuerySingleAsync<int>(insertVariantSql, variantParams, transaction);

                    string insertPriceSql = @"
                            INSERT INTO ProductPrices (VariantId, BasePrice, DiscountPrice, UpdatedAt)
                            VALUES (@VariantId, @BasePrice, @DiscountPrice, GETDATE());";

                    await connection.ExecuteAsync(insertPriceSql, new { VariantId = newVariantId, v.BasePrice, v.DiscountPrice }, transaction);

                    string insertStockSql = @"
                            INSERT INTO Stocks (VariantId, CurrentQuantity, ReservedQuantity, ReorderPoint, UpdatedAt)
                            VALUES (@VariantId, @CurrentQuantity, 0, @ReorderPoint, GETDATE());";

                    await connection.ExecuteAsync(insertStockSql, new { VariantId = newVariantId, v.CurrentQuantity, v.ReorderPoint }, transaction);

                    if (v.CurrentQuantity > 0)
                    {
                        string transactionSql = @"
                                INSERT INTO StockTransactions (VariantId, TransactionType, Quantity, UnitCost, Notes, CreatedBy)
                                VALUES (@VariantId, 'IN', @Quantity, @UnitCost, N'บันทึกยอดตั้งต้นจากการเพิ่ม SKU ใหม่ในโหมดแก้ไข', @CreatedBy);";

                        await connection.ExecuteAsync(transactionSql, new
                        {
                            VariantId = newVariantId,
                            Quantity = v.CurrentQuantity,
                            UnitCost = 0.00m,
                            CreatedBy = dto.UpdatedBy ?? dto.CreatedBy
                        }, transaction);
                    }
                }
                else
                {
                    // กรณีแก้ไขข้อมูลใน SKU ตัวเดิมที่มีอยู่แล้ว
                    string getOldImageSql = "SELECT ImageUrl FROM ProductVariants WHERE VariantId = @VariantId;";
                    string? oldImageUrl = await connection.ExecuteScalarAsync<string>(getOldImageSql, new { v.VariantId }, transaction);

                    // หากรูปเดิมมีการเปลี่ยนหรือโดนถอดออก ให้เก็บเข้าถังรูปขยะเพื่อเตรียมตรวจสอบ
                    if (!string.Equals(oldImageUrl, v.ImageUrl, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(oldImageUrl))
                    {
                        filesToCheckForDeletion.Add(oldImageUrl);
                    }

                    string updateVariantSql = @"
                            UPDATE ProductVariants 
                            SET Sku = @Sku, Barcode = @Barcode, UnitOfMeasure = @UnitOfMeasure,
                                Width = @Width, Length = @Length, Height = @Height, Weight = @Weight,
                                ImageUrl = @ImageUrl
                            WHERE VariantId = @VariantId;
                    
                            UPDATE ProductPrices 
                            SET BasePrice = @BasePrice, DiscountPrice = @DiscountPrice, UpdatedAt = GETDATE()
                            WHERE VariantId = @VariantId;

                            UPDATE Stocks 
                            SET CurrentQuantity = @CurrentQuantity, ReorderPoint = @ReorderPoint, UpdatedAt = GETDATE()
                            WHERE VariantId = @VariantId;";

                    await connection.ExecuteAsync(updateVariantSql, v, transaction);
                }
            }

            // ยืนยันการบันทึกฐานข้อมูลเสร็จสิ้นในรอบเดียว
            await transaction.CommitAsync();

            // 7. Garbage Collection: เคลียร์ไฟล์รูปภาพขยะทางกายภาพออกจากโฟลเดอร์ wwwroot จริง
            if (filesToCheckForDeletion.Any())
            {
                var webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

                foreach (var relativePath in filesToCheckForDeletion.Distinct())
                {
                    try
                    {
                        // ตรวจสอบกับฐานข้อมูลหลังการอัปเดตว่ายังมี SKU ตัวอื่นใช้งานภาพนี้ร่วมกันอยู่ไหม (ป้องกันกรณีรูปซ้ำ)
                        int usageCount = await connection.ExecuteScalarAsync<int>(
                            "SELECT COUNT(1) FROM ProductVariants WHERE ImageUrl = @ImageUrl;", new { ImageUrl = relativePath });

                        // ถ้ายอดการใช้งานเป็น 0 หมายความว่ารูปนี้ไม่มีใครใช้อีกต่อไป สามารถลบไฟล์ทิ้งได้ทันทีอย่างปลอดภัย
                        if (usageCount == 0)
                        {
                            var fullPath = Path.Combine(webRootPath, relativePath.TrimStart('/'));
                            if (System.IO.File.Exists(fullPath))
                            {
                                System.IO.File.Delete(fullPath);
                            }
                        }
                    }
                    catch { /* ดักจับข้ามเงื่อนไขเพื่อไม่ให้กระบวนการหลักเสียหาย */ }
                }
            }

            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}