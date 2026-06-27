using System.ComponentModel.DataAnnotations;

namespace PCS_API.DTOs;

public class ProductCreateDto
{
    [Required]
    [MaxLength(255)]
    public string ProductNameTh { get; set; } = null!;
    
    [MaxLength(255)]
    public string? ProductNameEn { get; set; }
    
    public string? Description { get; set; }
    
    [MaxLength(100)]
    public string? BrandName { get; set; }
    
    [Required]
    public int CategoryId { get; set; }
    
    [Required]
    [MaxLength(20)]
    public string ProductType { get; set; } = "Product";
    
    [Required]
    public bool IsStockTracked { get; set; } = true;

    [Required]
    [MaxLength(20)]
    public string InventoryGroup { get; set; } = "ForSale";

    [Required]
    [MaxLength(20)]
    public string ProductStatus { get; set; } = "Available";
    
    public int? CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }
    
    [Required]
    [MinLength(1, ErrorMessage = "At least one variant is required")]
    public List<VariantCreateDto> Variants { get; set; } = new();
}

public class VariantCreateDto
{
    public int? VariantId { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string Sku { get; set; } = null!;
    
    [MaxLength(50)]
    public string? Barcode { get; set; }

    [MaxLength(255)]
    public string? VariantNameTh { get; set; }

    [MaxLength(255)]
    public string? VariantNameEn { get; set; }
    
    [MaxLength(50)]
    public string? Color { get; set; }
    
    [MaxLength(50)]
    public string? SizeLabel { get; set; }
    
    [MaxLength(50)]
    public string? StylePattern { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string UnitOfMeasure { get; set; } = "อัน";
    
    [Range(0, 999999)]
    public decimal Width { get; set; } = 0.01m;
    public decimal Length { get; set; } = 0.01m;
    public decimal Height { get; set; } = 0.01m;
    public decimal Weight { get; set; } = 0.01m;

    public decimal BasePrice { get; set; } = 0.00m;
    public decimal DiscountPrice { get; set; } = 0.00m;

    public int CurrentQuantity { get; set; } = 0;
    public int ReorderPoint { get; set; } = 0;

    public string? ImageUrl { get; set; }
}