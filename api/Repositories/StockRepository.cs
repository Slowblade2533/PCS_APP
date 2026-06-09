using Microsoft.Data.SqlClient;
using PCS_API.DTOs;
using Dapper;
using PCS_API.Models;
using System.Data;

namespace PCS_API.Repositories;

public class StockRepository : IStockRepository
{
    private readonly string _connectionString;
    public StockRepository(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("DefaultConnection")!;
    }

    public async Task<int> CreateTransactionAsync(StockTransactionModel tx, IDbTransaction transaction)
    {
        var conn = transaction.Connection!;
        string sql = @"
            INSERT INTO dbo.StockTransactions (VariantId, TransactionType, Quantity, UnitCost, ReferenceDoc, Notes, CreatedAt, CreatedBy, RequestId, BranchId, QuantityBefore, QuantityAfter)
            VALUES (@VariantId, @TransactionType, @Quantity, @UnitCost, @ReferenceDoc, @Notes, GETDATE(), @CreatedBy, @RequestId, @BranchId, @QuantityBefore, @QuantityAfter);
            SELECT CAST(SCOPE_IDENTITY() as int);";

        return await conn.ExecuteScalarAsync<int>(sql, tx, transaction: transaction);
    }

    public async Task<PagedResultDto<StockDto>> GetStocksPagedAsync(StockSearchDto search)
    {
        await using var conn = new SqlConnection(_connectionString);

        string whereClause = "WHERE 1=1";
        var parameters = new DynamicParameters();
        
        if (!string.IsNullOrEmpty(search.SearchTerm))
        {
            whereClause += " AND (p.ProductNameTh LIKE @SearchTerm OR p.ProductNameEn LIKE @SearchTerm OR p.BrandName LIKE @SearchTerm OR v.Sku LIKE @SearchTerm OR v.Barcode LIKE @SearchTerm)";
            parameters.Add("SearchTerm", $"%{search.SearchTerm}%");
        }
        if (!string.IsNullOrEmpty(search.ProductStatus))
        {
            whereClause += " AND p.ProductStatus = @ProductStatus";
            parameters.Add("ProductStatus", search.ProductStatus);
        }
        if (!string.IsNullOrEmpty(search.ProductType))
        {
            whereClause += " AND p.ProductType = @ProductType";
            parameters.Add("ProductType", search.ProductType);
        }
        if (!string.IsNullOrEmpty(search.InventoryGroup))
        {
            whereClause += " AND p.InventoryGroup = @InventoryGroup";
            parameters.Add("InventoryGroup", search.InventoryGroup);
        }

        // TODO: Handle actual branch filtering. Right now it just shows stock from Stocks table,
        // but currently Stocks table doesn't have BranchId, it gets it from subquery.
        // If Branches logic requires filtering by BranchId on the Stocks or Transactions level, 
        // it needs to be added here. Assuming Stocks is global or BranchId is added later.

        string sql = $@"
            SELECT COUNT(*)
            FROM dbo.Stocks s
            INNER JOIN dbo.ProductVariants v ON s.VariantId = v.VariantId
            INNER JOIN dbo.Products p ON v.ProductId = p.ProductId
            {whereClause};

            SELECT s.VariantId, v.Sku, v.Barcode, p.ProductNameTh AS ProductName, 
                   COALESCE(NULLIF(v.VariantNameTh, ''), NULLIF(LTRIM(RTRIM(CONCAT(v.Color, ' ', v.SizeLabel, ' ', v.StylePattern))), ''), '') AS VariantName,
                   s.CurrentQuantity, s.ReservedQuantity, s.AvailableQuantity, 
                   s.ReorderPoint, s.UpdatedAt, 
                   (SELECT TOP 1 Id FROM dbo.Branches WHERE IsActive = 1) AS BranchId,
                   (SELECT TOP 1 BranchName FROM dbo.Branches WHERE IsActive = 1) AS BranchName
            FROM dbo.Stocks s
            INNER JOIN dbo.ProductVariants v ON s.VariantId = v.VariantId
            INNER JOIN dbo.Products p ON v.ProductId = p.ProductId
            {whereClause}
            ORDER BY v.Sku
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

        parameters.Add("Offset", search.GetSafeOffset());
        parameters.Add("PageSize", search.PageSize);

        using var multi = await conn.QueryMultipleAsync(sql, parameters);
        int totalCount = await multi.ReadSingleAsync<int>();
        var items = await multi.ReadAsync<StockDto>();

        return new PagedResultDto<StockDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = search.PageNumber,
            PageSize = search.PageSize
        };
    }

    public async Task<PagedResultDto<StockTransactionHistoryDto>> GetTransactionsAsync(string? transactionType, PaginationParamsDto @params)
    {
        await using var conn = new SqlConnection(_connectionString);
        var parameters = new DynamicParameters();

        parameters.Add("Offset", @params.GetSafeOffset());
        parameters.Add("PageSize", @params.PageSize);

        string whereClause = "";
        if (!string.IsNullOrEmpty(transactionType))
        {
            whereClause = "WHERE UPPER(t.TransactionType) = @TransactionType";
            parameters.Add("TransactionType", transactionType.ToUpper());
        }

        // ใช้ QueryMultipleAsync ส่ง COUNT + DATA ในรอบเดียว
        string sql = $@"
            SELECT COUNT(*) FROM dbo.StockTransactions t {whereClause};

            SELECT t.TransactionId, t.VariantId, v.Sku, v.Barcode, p.ProductNameTh AS ProductName,
                   COALESCE(NULLIF(v.VariantNameTh, ''), NULLIF(LTRIM(RTRIM(CONCAT(v.Color, ' ', v.SizeLabel, ' ', v.StylePattern))), ''), '') AS VariantName,
                   t.TransactionType, t.Quantity, t.CreatedAt, b.BranchName
            FROM dbo.StockTransactions t
            INNER JOIN dbo.ProductVariants v ON t.VariantId = v.VariantId
            INNER JOIN dbo.Products p ON v.ProductId = p.ProductId
            LEFT JOIN dbo.Branches b ON t.BranchId = b.Id
            {whereClause}
            ORDER BY t.CreatedAt DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

        using var multi = await conn.QueryMultipleAsync(sql, parameters);
        int totalCount = await multi.ReadSingleAsync<int>();
        var items = await multi.ReadAsync<StockTransactionHistoryDto>();

        return new PagedResultDto<StockTransactionHistoryDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = @params.PageNumber,
            PageSize = @params.PageSize
        };
    }

    public async Task<(int Before, int After)> UpdateStockQuantityAsync(int variantId, string transactionType, int qtyChange, IDbTransaction transaction)
    {
        var conn = transaction.Connection!;
        const int maxRetries = 3;
        var type = transactionType.ToUpper();

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            var stock = await conn.QuerySingleOrDefaultAsync<StockSnapshotModel>(
                @"SELECT CurrentQuantity, ReservedQuantity, RowVersion 
                  FROM dbo.Stocks WHERE VariantId = @variantId",
                new { variantId }, transaction);
            if (stock == null)
            {
                throw new KeyNotFoundException("ไม่พบข้อมูลสต็อกสำหรับสินค้านี้");
            }

            int beforeQuantity;
            int newQuantity;
            string updateQuery;
            object updateParams;

            if (type == "RESERVE" || type == "UNRESERVE")
            {
                beforeQuantity = stock.ReservedQuantity;
                newQuantity = stock.ReservedQuantity + qtyChange;
                
                if (newQuantity < 0)
                    throw new InvalidOperationException("ยอดจองติดลบไม่ได้");
                    
                if (stock.CurrentQuantity - newQuantity < 0)
                    throw new InvalidOperationException($"จองสต็อกเกินจำนวน Available (Current={stock.CurrentQuantity}, NewReserved={newQuantity})");

                updateQuery = @"
                    UPDATE dbo.Stocks
                    SET ReservedQuantity = @newQuantity, UpdatedAt = GETDATE()
                    WHERE VariantId = @variantId AND RowVersion = @rowVersion";
                
                updateParams = new { variantId, newQuantity, rowVersion = stock.RowVersion };
            }
            else
            {
                beforeQuantity = stock.CurrentQuantity;
                
                // สำหรับ ADJUST, qtyChange คือยอดที่ต้องการจะปรับให้เป็น (Absolute)
                // ดังนั้นเราต้องคำนวณส่วนต่างจริงๆ เพื่อ return และเพื่อ check logic
                int actualChange = type == "ADJUST" ? (qtyChange - stock.CurrentQuantity) : qtyChange;
                newQuantity = stock.CurrentQuantity + actualChange;

                if (newQuantity < 0)
                {
                    throw new InvalidOperationException("สต็อกไม่เพียงพอ (CurrentQuantity จะติดลบ)");
                }

                if (newQuantity < stock.ReservedQuantity)
                {
                    throw new InvalidOperationException(
                        $"สต็อกไม่เพียงพอ AvailableQuantity จะติดลบ " +
                        $"(CurrentQuantity={newQuantity}, ReservedQuantity={stock.ReservedQuantity})");
                }

                updateQuery = @"
                    UPDATE dbo.Stocks
                    SET CurrentQuantity = @newQuantity, UpdatedAt = GETDATE()
                    WHERE VariantId = @variantId AND RowVersion = @rowVersion";

                updateParams = new { variantId, newQuantity, rowVersion = stock.RowVersion };
            }

            var affectedRows = await conn.ExecuteAsync(updateQuery, updateParams, transaction);

            if (affectedRows > 0)
            {
                // ส่งคืน (ยอดก่อน, ยอดหลัง)
                return (beforeQuantity, newQuantity);
            }
        }

        throw new InvalidOperationException("ข้อมูลสต็อกถูกแก้ไขโดยผู้ใช้อื่น กรุณาลองใหม่อีกครั้ง");
    }

    public async Task<bool> VariantExistsAsync(int variantId, IDbTransaction transaction)
    {
        var conn = transaction.Connection!;
        const string sql = @"SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.ProductVariants WHERE VariantId = @variantId) THEN 1 ELSE 0 END";
        return await conn.ExecuteScalarAsync<bool>(sql, new { variantId }, transaction);
    }

    public async Task<bool> TransactionExistsByRequestIdAsync(Guid requestId, IDbTransaction transaction)
    {
        var conn = transaction.Connection!;
        const string sql = @"SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.StockTransactions WHERE RequestId = @requestId) THEN 1 ELSE 0 END";
        return await conn.ExecuteScalarAsync<bool>(sql, new { requestId }, transaction);
    }
}