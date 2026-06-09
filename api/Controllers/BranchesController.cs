using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace PCS_API.Controllers;

public record BranchDto(int Id, string BranchCode, string BranchName, bool IsActive);

public class BranchDetailDto
{
    public int Id { get; set; }
    public string BranchName { get; set; } = null!;
    public string? Address { get; set; }
    public bool IsActive { get; set; }
    public string? TaxId { get; set; }
    public string? RegistrationName { get; set; }
    public string? CompanyType { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? LogoUrl { get; set; }
    public string BranchCode { get; set; } = null!;
    public bool IsVatRegistered { get; set; }
    public string? VatDocumentUrl { get; set; }
    public string EntityType { get; set; } = "Individual";
}

public class BranchUpdateDto
{
    public string BranchName { get; set; } = null!;
    public string? Address { get; set; }
    public string? TaxId { get; set; }
    public string? RegistrationName { get; set; }
    public string? CompanyType { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? LogoUrl { get; set; }
    public string BranchCode { get; set; } = null!;
    public bool IsVatRegistered { get; set; }
    public string? VatDocumentUrl { get; set; }
    public string EntityType { get; set; } = "Individual";
}

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BranchesController : ControllerBase
{
    private readonly string _connectionString;
    public BranchesController(IConfiguration config) => _connectionString = config.GetConnectionString("DefaultConnection")!;

    [HttpGet]
    public async Task<IActionResult> GetBranches()
    {
        await using var conn = new SqlConnection(_connectionString);
        const string sql = "SELECT Id, BranchCode, BranchName, IsActive FROM dbo.Branches WHERE IsActive = 1 ORDER BY BranchName";
        var branches = await conn.QueryAsync<BranchDto>(sql);
        return Ok(branches);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetBranch(int id)
    {
        await using var conn = new SqlConnection(_connectionString);
        const string sql = "SELECT * FROM dbo.Branches WHERE Id = @Id";
        var branch = await conn.QueryFirstOrDefaultAsync<BranchDetailDto>(sql, new { Id = id });
        if (branch == null) return NotFound(new { message = "Branch not found" });
        return Ok(branch);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateBranch(int id, [FromBody] BranchUpdateDto dto)
    {
        await using var conn = new SqlConnection(_connectionString);
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
        
        if (rows == 0) return NotFound(new { message = "Branch not found" });
        return Ok(new { message = "Branch updated successfully" });
    }
}