using System.ComponentModel.DataAnnotations;

namespace PCS_API.DTOs;

public class StockDto
{
    public int VariantId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string VariantName { get; set; } = string.Empty;
    public int CurrentQuantity { get; set; }
    public int ReservedQuantity { get; set; }
    public int AvailableQuantity { get; set; }
    public int ReorderPoint { get; set; }
    public int? MaxStockLevel { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    public int? BranchId { get; set; }
    public string? BranchName { get; set; }
    public string? ImageUrl { get; set; }
    public string? BrandName { get; set; }
    public string Condition { get; set; } = "Normal";
}

public class StockSearchDto : PaginationParamsDto
{
    public string? SearchTerm { get; set; }
    public string? ProductStatus { get; set; }
    public string? ProductType { get; set; }
    public string? InventoryGroup { get; set; }
    public int? BranchId { get; set; }
    public string? Condition { get; set; }
}

public class CreateStockTransactionDto
{
    [Required(ErrorMessage = "RequestId จำเป็นต้องระบุ (Idempotency Key)")]
    public Guid RequestId { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "VariantId ต้องมากกว่า 0")]
    public int VariantId { get; set; }

    [Required(ErrorMessage = "กรุณาระบุสาขา")]
    public int BranchId { get; set; }

    [Required]
    [RegularExpression("^(IN|OUT|ADJUST|RESERVE|UNRESERVE|DAMAGE|LOST)$", ErrorMessage = "TransactionType ต้องเป็น IN, OUT, ADJUST, RESERVE, UNRESERVE, DAMAGE หรือ LOST")]
    public string TransactionType { get; set; } = "IN";

    [Required]
    [RegularExpression("^(Normal|Defect|Damage)$", ErrorMessage = "Condition ต้องเป็น Normal, Defect หรือ Damage")]
    public string Condition { get; set; } = "Normal";

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Quantity ต้องมากกว่า 0")]
    public int Quantity { get; set; }

    [Range(0, (double)decimal.MaxValue, ErrorMessage = "UnitCost ต้องไม่ติดลบ")]
    public decimal? UnitCost { get; set; }

    [MaxLength(50)]
    public string? ReferenceDoc { get; set; }

    [MaxLength(255)]
    public string? Notes { get; set; }
}

public class StockTransactionHistoryDto
{
    public long TransactionId { get; set; }
    public int VariantId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string VariantName { get; set; } = string.Empty;
    public string TransactionType { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal? UnitCost { get; set; }
    public string? ReferenceDoc { get; set; }
    public string? Notes { get; set; }
    public DateTime? CreatedAt { get; set; }
    public string? CreatedByName { get; set; }

    public int? BranchId { get; set; }
    public string? BranchName { get; set; }
    public int? QuantityBefore { get; set; }
    public int? QuantityAfter { get; set; }
    public string Condition { get; set; } = "Normal";
}