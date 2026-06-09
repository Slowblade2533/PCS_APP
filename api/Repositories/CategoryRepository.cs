using Dapper;
using Microsoft.Data.SqlClient;
using PCS_API.Models;

namespace PCS_API.Repositories;

public class CategoryRepository(IConfiguration config) : ICategoryRepository
{
    private readonly string _connectionString = config.GetConnectionString("DefaultConnection")!;

    public async Task<IEnumerable<CategoryModel>> GetAllCategoriesAsync(string? searchTerm = null)
    {
        await using var conn = new SqlConnection(_connectionString);
        var query = @"
            WITH CategoryCTE AS (
                SELECT 
                    CategoryId, CategoryName, Description, ParentId, SortOrder, IsActive,
                    CAST(CategoryName AS NVARCHAR(MAX)) AS FullPath,
                    1 AS Level,
                    CAST(NULL AS NVARCHAR(200)) AS ParentName
                FROM dbo.Categories
                WHERE ParentId IS NULL
                
                UNION ALL
                
                SELECT 
                    c.CategoryId, c.CategoryName, c.Description, c.ParentId, c.SortOrder, c.IsActive,
                    cte.FullPath + ' > ' + c.CategoryName,
                    cte.Level + 1,
                    CAST(cte.CategoryName AS NVARCHAR(200)) AS ParentName
                FROM dbo.Categories c
                INNER JOIN CategoryCTE cte ON c.ParentId = cte.CategoryId
            )
            SELECT * FROM CategoryCTE
            WHERE (@SearchTerm IS NULL OR 
                   CategoryName LIKE '%' + @SearchTerm + '%' OR 
                   FullPath LIKE '%' + @SearchTerm + '%')
            ORDER BY FullPath, SortOrder, CategoryName;
        ";
        
        return await conn.QueryAsync<CategoryModel>(query, new { SearchTerm = searchTerm });
    }

    public async Task<CategoryModel?> GetCategoryByIdAsync(int id)
    {
        await using var conn = new SqlConnection(_connectionString);
        var query = @"
            WITH CategoryCTE AS (
                SELECT 
                    CategoryId, CategoryName, Description, ParentId, SortOrder, IsActive,
                    CAST(CategoryName AS NVARCHAR(MAX)) AS FullPath,
                    1 AS Level,
                    CAST(NULL AS NVARCHAR(200)) AS ParentName
                FROM dbo.Categories
                WHERE ParentId IS NULL
                
                UNION ALL
                
                SELECT 
                    c.CategoryId, c.CategoryName, c.Description, c.ParentId, c.SortOrder, c.IsActive,
                    cte.FullPath + ' > ' + c.CategoryName,
                    cte.Level + 1,
                    CAST(cte.CategoryName AS NVARCHAR(200)) AS ParentName
                FROM dbo.Categories c
                INNER JOIN CategoryCTE cte ON c.ParentId = cte.CategoryId
            )
            SELECT * FROM CategoryCTE WHERE CategoryId = @Id;
        ";
        
        return await conn.QuerySingleOrDefaultAsync<CategoryModel>(query, new { Id = id });
    }

    public async Task<int> CreateCategoryAsync(CategoryModel category)
    {
        await using var conn = new SqlConnection(_connectionString);
        var query = @"
            INSERT INTO dbo.Categories (CategoryName, Description, ParentId, SortOrder, IsActive)
            OUTPUT INSERTED.CategoryId
            VALUES (@CategoryName, @Description, @ParentId, @SortOrder, @IsActive);
        ";
        
        return await conn.ExecuteScalarAsync<int>(query, category);
    }

    public async Task<bool> UpdateCategoryAsync(CategoryModel category)
    {
        await using var conn = new SqlConnection(_connectionString);
        var query = @"
            UPDATE dbo.Categories
            SET CategoryName = @CategoryName,
                Description = @Description,
                ParentId = @ParentId,
                SortOrder = @SortOrder,
                IsActive = @IsActive
            WHERE CategoryId = @CategoryId;
        ";
        
        var rows = await conn.ExecuteAsync(query, category);
        return rows > 0;
    }

    public async Task<bool> DeleteCategoryAsync(int id)
    {
        await using var conn = new SqlConnection(_connectionString);
        var query = "DELETE FROM dbo.Categories WHERE CategoryId = @Id;";
        var rows = await conn.ExecuteAsync(query, new { Id = id });
        return rows > 0;
    }

    public async Task<bool> HasChildrenAsync(int id)
    {
        await using var conn = new SqlConnection(_connectionString);
        var query = "SELECT TOP 1 1 FROM dbo.Categories WHERE ParentId = @Id;";
        var result = await conn.ExecuteScalarAsync<int?>(query, new { Id = id });
        return result.HasValue;
    }
}
