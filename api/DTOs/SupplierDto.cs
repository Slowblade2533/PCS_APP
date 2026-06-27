using System.ComponentModel.DataAnnotations;

namespace PCS_API.DTOs;

public class SupplierDto
{
    public int Id { get; set; }
    public string SupplierCode { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string? ContactName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? TaxId { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class SupplierCreateDto
{
    [Required]
    [MaxLength(50)]
    public string SupplierCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string SupplierName { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? ContactName { get; set; }

    [MaxLength(50)]
    public string? Phone { get; set; }

    [MaxLength(255)]
    [EmailAddress]
    public string? Email { get; set; }

    public string? Address { get; set; }

    [MaxLength(50)]
    public string? TaxId { get; set; }

    public bool IsActive { get; set; } = true;
}

public class SupplierSearchDto : PaginationParamsDto
{
    public string? SearchTerm { get; set; }
    public bool? IsActive { get; set; }
}
