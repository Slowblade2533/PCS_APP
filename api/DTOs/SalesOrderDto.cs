using System.ComponentModel.DataAnnotations;

namespace PCS_API.DTOs;

// ─── Sales Order ──────────────────────────────────────────────────────────────
public class SalesOrderListDto
{
    public int OrderId { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public string? CustomerName { get; set; }
    public decimal GrandTotal { get; set; }
    public string OrderStatus { get; set; } = string.Empty;
    public string? PaymentMethod { get; set; }
    public int ItemCount { get; set; }
}

public class SalesOrderDetailDto
{
    public int OrderId { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public string? CustomerTaxId { get; set; }
    public string? CustomerAddress { get; set; }
    public decimal SubTotal { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal VatRate { get; set; }
    public decimal VatAmount { get; set; }
    public decimal GrandTotal { get; set; }
    public string OrderStatus { get; set; } = string.Empty;
    public int? TaxInvoiceId { get; set; }
    public string? PaymentMethod { get; set; }
    public string? PaymentRefNo { get; set; }
    public string? ReceiverAccountName { get; set; }
    public string? SlipAttachmentUrl { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<SalesOrderItemDto> Items { get; set; } = new();
}

public class SalesOrderItemDto
{
    public long OrderItemId { get; set; }
    public int VariantId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string? VariantName { get; set; }
    public string? ImageUrl { get; set; }
    public string Condition { get; set; } = "Normal";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Discount { get; set; }
    public decimal LineTotal { get; set; }
}

public class SalesOrderCreateDto
{
    public DateTime OrderDate { get; set; } = DateTime.Now;

    [MaxLength(255)]
    public string? CustomerName { get; set; }

    [MaxLength(50)]
    public string? CustomerPhone { get; set; }

    [MaxLength(20)]
    public string? CustomerTaxId { get; set; }

    [MaxLength(500)]
    public string? CustomerAddress { get; set; }

    public decimal VatRate { get; set; } = 0;

    [MaxLength(20)]
    public string? PaymentMethod { get; set; }

    [MaxLength(100)]
    public string? PaymentRefNo { get; set; }

    public int? ReceiverAccountId { get; set; }

    [MaxLength(500)]
    public string? SlipAttachmentUrl { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public int? CreatedBy { get; set; }

    [Required, MinLength(1, ErrorMessage = "ต้องมีรายการสินค้าอย่างน้อย 1 รายการ")]
    public List<SalesOrderItemCreateDto> Items { get; set; } = new();
}

public class SalesOrderItemCreateDto
{
    [Required]
    public int VariantId { get; set; }

    [MaxLength(50)]
    public string Condition { get; set; } = "Normal";

    [Required, Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [Required, Range(0, double.MaxValue)]
    public decimal UnitPrice { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Discount { get; set; } = 0;
}

public class SalesOrderSearchDto : PaginationParamsDto
{
    public string? SearchTerm { get; set; }
    public string? Status { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
}
