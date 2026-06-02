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
            // 1. Insert ลงตาราง Products และรับ ProductId กลับมา (OUTPUT INSERTED.ProductId หรือ SCOPE_IDENTITY())
            string productSql = @"
                    INSERT INTO Products (ProductNameTh, ProductNameEn, Description, BrandName, CategoryId, ProductType, ProductStatus, CreatedBy)
                    OUTPUT INSERTED.ProductId
                    VALUES (@ProductNameTh, @ProductNameEn, @Description, @BrandName, @CategoryId, @ProductType, @ProductStatus, @CreatedBy);";

            int productId = await connection.QuerySingleAsync<int>(productSql, dto, transaction);

            // 2. Loop เพื่อ Insert Variants และตารางที่เกี่ยวข้อง
            foreach (var v in dto.Variants)
            {
                // 2.1 Insert ลงตาราง ProductVariants
                string variantSql = @"
                        INSERT INTO ProductVariants (ProductId, Sku, Barcode, UnitOfMeasure, Width, Length, Height, Weight)
                        OUTPUT INSERTED.VariantId
                        VALUES (@ProductId, @Sku, @Barcode, @UnitOfMeasure, @Width, @Length, @Height, @Weight);";

                // สร้าง dynamic parameters เพื่อส่ง ProductId ที่ได้จากข้อ 1 ไปด้วย
                var variantParams = new DynamicParameters(v);
                variantParams.Add("ProductId", productId);

                int variantId = await connection.QuerySingleAsync<int>(variantSql, variantParams, transaction);

                // 2.2 Insert ลงตาราง ProductPrices
                string priceSql = @"
                        INSERT INTO ProductPrices (VariantId, BasePrice, DiscountPrice, UpdatedAt)
                        VALUES (@VariantId, @BasePrice, @DiscountPrice, GETDATE());";

                await connection.ExecuteAsync(priceSql, new { VariantId = variantId, v.BasePrice, v.DiscountPrice }, transaction);

                // 2.3 Insert ลงตาราง Stocks
                string stockSql = @"
                        INSERT INTO Stocks (VariantId, CurrentQuantity, ReservedQuantity, ReorderPoint, UpdatedAt)
                        VALUES (@VariantId, @CurrentQuantity, 0, @ReorderPoint, GETDATE());";

                await connection.ExecuteAsync(stockSql, new { VariantId = variantId, v.CurrentQuantity, v.ReorderPoint }, transaction);

                // 2.4 (ออฟชันเสริม) บันทึกประวัติสต็อกเริ่มต้นเข้าตาราง StockTransactions
                if (v.CurrentQuantity > 0)
                {
                    string transactionSql = @"
                            INSERT INTO StockTransactions (VariantId, TransactionType, Quantity, UnitCost, Notes, CreatedBy)
                            VALUES (@VariantId, 'IN', @Quantity, @UnitCost, N'บันทึกยอดตั้งต้นจากการเพิ่มสินค้าใหม่', @CreatedBy);";

                    await connection.ExecuteAsync(transactionSql, new
                    {
                        VariantId = variantId,
                        Quantity = v.CurrentQuantity,
                        UnitCost = 0.00m, // หรือใส่ BasePrice แล้วแต่ธุรกิจ
                        CreatedBy = dto.CreatedBy
                    }, transaction);
                }
            }

            // หากทุกอย่างผ่านพ้นไปด้วยดี ให้ Commit
            await transaction.CommitAsync();
            return productId;
        }
        catch
        {
            // หากมี Error ให้ Rollback ทั้งหมดทันที
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<PagedResult<ProductListDto>> GetPagedProductsAsync(ProductSearchParams search)
    {
        using var connection = new SqlConnection(_connectionString);

        // สร้าง SQL Query ที่ทำการ Join ข้อมูลหลัก ราคา และสต็อกมารวมกัน
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
            COUNT(*) OVER() AS TotalCount -- ดึงจำนวนแถวทั้งหมดที่ตรงตามเงื่อนไขเพื่อใช้คิดหน้าทั้งหมด
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

        // ดึงตัวเลขรวมจากคอลัมน์ TotalCount ของแถวแรก (ถ้าไม่มีข้อมูลให้เป็น 0)
        int totalCount = items.FirstOrDefault()?.TotalCount ?? 0;

        return new PagedResult<ProductListDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = search.PageNumber,
            PageSize = search.PageSize
        };
    }
}
