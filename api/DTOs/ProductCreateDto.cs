namespace PCS_API.DTOs;

public class ProductCreateDto
{
    public string ProductNameTh { get; set; } = null!;
    public string? ProductNameEn { get; set; }
    public string? Description { get; set; }
    public string? BrandName { get; set; }
    public int CategoryId { get; set; }
    public string ProductType { get; set; } = "Product";
    public string ProductStatus { get; set; } = "Available";
    public int? CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }
    public List<VariantCreateDto> Variants { get; set; } = new();
}

public class VariantCreateDto
{
    public int? VariantId { get; set; }
    public string Sku { get; set; } = null!;
    public string? Barcode { get; set; }
    public string UnitOfMeasure { get; set; } = "อัน";
    public decimal Width { get; set; } = 0.01m;
    public decimal Length { get; set; } = 0.01m;
    public decimal Height { get; set; } = 0.01m;
    public decimal Weight { get; set; } = 0.01m;

    // สำหรับตาราง ProductPrices
    public decimal BasePrice { get; set; } = 0.00m;
    public decimal DiscountPrice { get; set; } = 0.00m;

    // สำหรับตาราง Stocks
    public int CurrentQuantity { get; set; } = 0;
    public int ReorderPoint { get; set; } = 0;

    // สำหรับรูปภาพ
    public string? ImageUrl { get; set; }
}