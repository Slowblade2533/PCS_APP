namespace PCS_API.DTOs;

public class ProductSearchParamsDto : PaginationParamsDto
{
    public string? SearchTerm { get; set; }
    public int? CategoryId { get; set; }
    public string? ProductStatus { get; set; }
    public string? ProductType { get; set; }
    public string? InventoryGroup { get; set; }
}

public class ProductListDto
{
    public int ProductId { get; set; }
    public string ProductNameTh { get; set; } = null!;
    public string? ProductNameEn { get; set; }
    public string? BrandName { get; set; }
    public string CategoryName { get; set; } = null!;
    public string ProductType { get; set; } = null!;
    public bool IsStockTracked { get; set; }
    public string InventoryGroup { get; set; } = null!;
    public string ProductStatus { get; set; } = null!;
    public int TotalVariants { get; set; }
    public int TotalAvailableStock { get; set; }
    public decimal MinPrice { get; set; }
    public string? ImageUrl { get; set; }
}


public class ProductDetailDto
{
    public int ProductId { get; set; }
    public string ProductNameTh { get; set; } = null!;
    public string? ProductNameEn { get; set; }
    public string? Description { get; set; }
    public string? BrandName { get; set; }
    public int CategoryId { get; set; }
    public string ProductType { get; set; } = null!;
    public bool IsStockTracked { get; set; }
    public string InventoryGroup { get; set; } = null!;
    public string ProductStatus { get; set; } = null!;
    public List<ProductVariantDetailDto> Variants { get; set; } = new();
}

public class ProductVariantDetailDto
{
    public int VariantId { get; set; }
    public int ProductId { get; set; }
    public string Sku { get; set; } = null!;
    public string? Barcode { get; set; }
    public string? VariantNameTh { get; set; }
    public string? VariantNameEn { get; set; }
    public string? Color { get; set; }
    public string? SizeLabel { get; set; }
    public string? StylePattern { get; set; }
    public string? ImageUrl { get; set; }
    public string UnitOfMeasure { get; set; } = null!;
    public decimal Width { get; set; }
    public decimal Length { get; set; }
    public decimal Height { get; set; }
    public decimal Weight { get; set; }
    public decimal BasePrice { get; set; }
    public decimal DiscountPrice { get; set; }
    public int CurrentQuantity { get; set; }
    public int ReorderPoint { get; set; }
}

public class ResultDto<T>
{
    public bool IsSuccess { get; set; }
    public T? Value { get; set; }
    public string? ErrorMessage { get; set; }

    public static ResultDto<T> Success(T value) => new ResultDto<T> { IsSuccess = true, Value = value };
    public static ResultDto<T> Failure(string errorMessage) => new ResultDto<T> { IsSuccess = false, ErrorMessage = errorMessage };
}