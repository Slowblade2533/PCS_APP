using Dapper;
using PCS_API.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PCS_API.Repositories;

public class BranchRepository(ISqlConnectionFactory connectionFactory) : IBranchRepository
{
    public async Task<IEnumerable<BranchDto>> GetAllActiveAsync()
    {
        using var conn = connectionFactory.CreateConnection();
        const string sql = "SELECT Id, BranchCode, BranchName, IsActive FROM dbo.Branches WHERE IsActive = 1 ORDER BY BranchName";
        return await conn.QueryAsync<BranchDto>(sql);
    }

    public async Task<BranchDetailDto?> GetByIdAsync(int id)
    {
        using var conn = connectionFactory.CreateConnection();
        const string sql = "SELECT Id, BranchCode, BranchName, Address, TaxId, RegistrationName, CompanyType, Phone, Email, LogoUrl, IsVatRegistered, VatDocumentUrl, EntityType, IsActive FROM dbo.Branches WHERE Id = @Id";
        return await conn.QueryFirstOrDefaultAsync<BranchDetailDto>(sql, new { Id = id });
    }

    public async Task<bool> UpdateAsync(int id, BranchUpdateDto dto)
    {
        using var conn = connectionFactory.CreateConnection();
        const string sql = @"
            UPDATE dbo.Branches 
            SET BranchCode = @BranchCode, BranchName = @BranchName, Address = @Address, TaxId = @TaxId, 
                RegistrationName = @RegistrationName, CompanyType = @CompanyType, 
                Phone = @Phone, Email = @Email, LogoUrl = @LogoUrl,
                IsVatRegistered = @IsVatRegistered, VatDocumentUrl = @VatDocumentUrl, EntityType = @EntityType
            WHERE Id = @Id";
            
        var rows = await conn.ExecuteAsync(sql, new { 
            Id = id, 
            dto.BranchName, 
            dto.Address, 
            dto.TaxId, 
            dto.RegistrationName, 
            dto.CompanyType, 
            dto.Phone, 
            dto.Email, 
            dto.LogoUrl,
            dto.BranchCode,
            dto.IsVatRegistered,
            dto.VatDocumentUrl,
            dto.EntityType
        });
        
        return rows > 0;
    }
}
