using System.ComponentModel.DataAnnotations;

namespace PCS_API.DTOs;

public class VcbOrderDto
{
    public int Id { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public int BranchId { get; set; }
    public string? Notes { get; set; }
    public string? TransferSlipUrl { get; set; }
    public int? CreatedBy { get; set; }
    public string? CreatedByUsername { get; set; }
    public int? UpdatedBy { get; set; }
    public string? UpdatedByUsername { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int ItemCount { get; set; }
    public List<VcbOrderItemDto> Items { get; set; } = new();
}

public class VcbOrderItemDto
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int VariantId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string VariantName { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public int Quantity { get; set; }
    public decimal TotalPrice { get; set; }
    public int ReceivedQuantity { get; set; }
    public int RemainingQuantity { get; set; }
}

public class VcbOrderCreateDto
{
    [Required]
    public string OrderNo { get; set; } = string.Empty;
    [Required]
    public DateTime OrderDate { get; set; }
    [Required]
    public decimal TotalAmount { get; set; }
    [Required]
    public int BranchId { get; set; }
    public string? Notes { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "ต้องมีรายการสินค้าอย่างน้อย 1 รายการ")]
    public List<VcbOrderItemCreateDto> Items { get; set; } = new();
}

public class VcbOrderItemCreateDto
{
    [Required]
    public int VariantId { get; set; }
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "จำนวนต้องมากกว่า 0")]
    public int Quantity { get; set; }
    [Required]
    public decimal TotalPrice { get; set; }
}

public class VcbOrderSearchDto : PaginationParamsDto
{
    public string? SearchTerm { get; set; }
    public string? Status { get; set; }
    public int? BranchId { get; set; }
}
