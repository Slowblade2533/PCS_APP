namespace PCS_API.DTOs;

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
