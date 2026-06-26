using Dapper;
using PCS_API.DTOs;
using PCS_API.Models;

namespace PCS_API.Repositories;

public class SupplierRepository(ISqlConnectionFactory connectionFactory) : ISupplierRepository
{
    public async Task<SupplierModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        using var conn = connectionFactory.CreateConnection();
        var command = new CommandDefinition(
            "SELECT Id, SupplierCode, SupplierName, ContactName, Phone, Email, Address, TaxId, IsActive, CreatedAt, UpdatedAt FROM dbo.Suppliers WHERE Id = @Id",
            new { Id = id },
            cancellationToken: cancellationToken);
        return await conn.QueryFirstOrDefaultAsync<SupplierModel>(command);
    }

    public async Task<PagedResultDto<SupplierDto>> GetPagedAsync(SupplierSearchDto search, CancellationToken cancellationToken = default)
    {
        using var conn = connectionFactory.CreateConnection();
        var parameters = new DynamicParameters();
        string whereClause = "WHERE 1=1";

        if (!string.IsNullOrEmpty(search.SearchTerm))
        {
            whereClause += " AND (SupplierCode LIKE @SearchTerm OR SupplierName LIKE @SearchTerm OR Phone LIKE @SearchTerm)";
            parameters.Add("SearchTerm", $"%{search.SearchTerm}%");
        }

        if (search.IsActive.HasValue)
        {
            whereClause += " AND IsActive = @IsActive";
            parameters.Add("IsActive", search.IsActive.Value);
        }

        string sql = $@"
            SELECT COUNT(*) FROM dbo.Suppliers {whereClause};

            SELECT Id, SupplierCode, SupplierName, ContactName, Phone, Email, Address, TaxId, IsActive, CreatedAt, UpdatedAt FROM dbo.Suppliers
            {whereClause}
            ORDER BY Id DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

        parameters.Add("Offset", search.GetSafeOffset());
        parameters.Add("PageSize", search.PageSize);

        var command = new CommandDefinition(sql, parameters, cancellationToken: cancellationToken);
        using var multi = await conn.QueryMultipleAsync(command);
        int totalCount = await multi.ReadSingleAsync<int>();
        var items = await multi.ReadAsync<SupplierDto>();

        return new PagedResultDto<SupplierDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = search.PageNumber,
            PageSize = search.PageSize
        };
    }

    public async Task<int> CreateAsync(SupplierModel model, CancellationToken cancellationToken = default)
    {
        using var conn = connectionFactory.CreateConnection();
        string sql = @"
            INSERT INTO dbo.Suppliers (SupplierCode, SupplierName, ContactName, Phone, Email, Address, TaxId, IsActive, CreatedAt, UpdatedAt)
            VALUES (@SupplierCode, @SupplierName, @ContactName, @Phone, @Email, @Address, @TaxId, @IsActive, GETDATE(), GETDATE());
            SELECT CAST(SCOPE_IDENTITY() as int);";

        var command = new CommandDefinition(sql, model, cancellationToken: cancellationToken);
        return await conn.QuerySingleAsync<int>(command);
    }

    public async Task<bool> UpdateAsync(SupplierModel model, CancellationToken cancellationToken = default)
    {
        using var conn = connectionFactory.CreateConnection();
        string sql = @"
            UPDATE dbo.Suppliers
            SET SupplierCode = @SupplierCode,
                SupplierName = @SupplierName,
                ContactName = @ContactName,
                Phone = @Phone,
                Email = @Email,
                Address = @Address,
                TaxId = @TaxId,
                IsActive = @IsActive,
                UpdatedAt = GETDATE()
            WHERE Id = @Id;";

        var command = new CommandDefinition(sql, model, cancellationToken: cancellationToken);
        int rows = await conn.ExecuteAsync(command);
        return rows > 0;
    }

    public async Task<bool> ExistsByCodeAsync(string code, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        using var conn = connectionFactory.CreateConnection();
        string sql = "SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.Suppliers WHERE SupplierCode = @Code";
        if (excludeId.HasValue)
        {
            sql += " AND Id != @ExcludeId";
        }
        sql += ") THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END;";

        var command = new CommandDefinition(sql, new { Code = code, ExcludeId = excludeId }, cancellationToken: cancellationToken);
        return await conn.QuerySingleAsync<bool>(command);
    }
}
