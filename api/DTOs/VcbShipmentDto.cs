using System.ComponentModel.DataAnnotations;

namespace PCS_API.DTOs;

public class VcbShipmentDto
{
    public int Id { get; set; }
    public string ReceiptNo { get; set; } = string.Empty;
    public DateTime ReceiptDate { get; set; }
    public int DeliveryId { get; set; }
    public string? DeliveryNo { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public int? CreatedBy { get; set; }
    public string? CreatedByUsername { get; set; }
    public int? UpdatedBy { get; set; }
    public string? UpdatedByUsername { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int ItemsCount { get; set; }
    public bool IsForceCloseOrder { get; set; }

    public List<VcbShipmentItemDto> Items { get; set; } = new();
}

public class VcbShipmentItemDto
{
    public int Id { get; set; }
    public int ShipmentId { get; set; }
    public int OrderItemId { get; set; }
    public int VariantId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string VariantName { get; set; } = string.Empty;
    public string? BoxNumbers { get; set; }
    public string ReceiptStatus { get; set; } = string.Empty;
    public int ExpectedQuantity { get; set; }
    public int GoodQuantity { get; set; }
    public int DefectiveQuantity { get; set; }
    public decimal RefundAmount { get; set; }
}

public class VcbShipmentCreateDto
{
    [Required]
    public int DeliveryId { get; set; }
    public string? Notes { get; set; }
    public bool IsForceCloseOrder { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "ต้องมีรายการพัสดุอย่างน้อย 1 รายการ")]
    public List<VcbShipmentItemCreateDto> Items { get; set; } = new();
}

public class VcbShipmentItemCreateDto
{
    [Required]
    public int OrderItemId { get; set; }
    [Required]
    public int VariantId { get; set; }
    [Required]
    public string BoxNumbers { get; set; } = string.Empty;
    [Required]
    public string ReceiptStatus { get; set; } = string.Empty;
    [Required]
    public int ExpectedQuantity { get; set; }
    public int GoodQuantity { get; set; }
    public int DefectiveQuantity { get; set; }
    public decimal RefundAmount { get; set; }
}

public class VcbShipmentSearchDto : PaginationParamsDto
{
    public string? SearchTerm { get; set; }
    public string? Status { get; set; }
    public int? DeliveryId { get; set; }
}
