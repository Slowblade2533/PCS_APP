using System.ComponentModel.DataAnnotations;

namespace PCS_API.DTOs;

// ─── Purchase Order List (for table/paged view) ───────────────────────────────
public class PurchaseOrderListDto
{
    public int PurchaseOrderId { get; set; }
    public string PONo { get; set; } = string.Empty;
    public DateOnly PODate { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public decimal GrandTotal { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? PaymentMethod { get; set; }
    public int ItemCount { get; set; }
    public int TotalOrdered { get; set; }
    public int TotalReceived { get; set; }
}

// ─── Purchase Order Detail (full view with items) ─────────────────────────────
public class PurchaseOrderDetailDto
{
    public int PurchaseOrderId { get; set; }
    public string PONo { get; set; } = string.Empty;
    public DateOnly PODate { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string? SupplierPhone { get; set; }
    public string? SupplierTaxId { get; set; }
    public string? SupplierAddress { get; set; }
    public decimal SubTotal { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal VatRate { get; set; }
    public decimal VatAmount { get; set; }
    public decimal GrandTotal { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? TaxInvoiceId { get; set; }
    public DateOnly? ExpectedDeliveryDate { get; set; }
    public string? PaymentMethod { get; set; }
    public string? PaymentRefNo { get; set; }
    public string? SourceAccountInfo { get; set; }
    public string? ReceiverAccountName { get; set; }
    public string? SlipAttachmentUrl { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<PurchaseOrderItemDto> Items { get; set; } = new();
}

public class PurchaseOrderItemDto
{
    public long POItemId { get; set; }
    public int VariantId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string? VariantName { get; set; }
    public string? ImageUrl { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public int ReceivedQuantity { get; set; }
}

// ─── Create / Update Purchase Order ──────────────────────────────────────────
public class PurchaseOrderCreateDto
{
    [Required, MaxLength(50)]
    public string PONo { get; set; } = string.Empty;

    [Required]
    public DateOnly PODate { get; set; }

    [Required, MaxLength(255)]
    public string SupplierName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? SupplierPhone { get; set; }

    [MaxLength(20)]
    public string? SupplierTaxId { get; set; }

    [MaxLength(500)]
    public string? SupplierAddress { get; set; }

    public decimal DiscountTotal { get; set; } = 0;
    public decimal ShippingCost { get; set; } = 0;
    public decimal VatRate { get; set; } = 0;
    public DateOnly? ExpectedDeliveryDate { get; set; }

    // Payment details
    [MaxLength(20)]
    public string? PaymentMethod { get; set; }

    [MaxLength(100)]
    public string? PaymentRefNo { get; set; }

    [MaxLength(255)]
    public string? SourceAccountInfo { get; set; }

    public int? ReceiverAccountId { get; set; }

    [MaxLength(500)]
    public string? SlipAttachmentUrl { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public int? CreatedBy { get; set; }

    [Required, MinLength(1, ErrorMessage = "ต้องมีรายการสินค้าอย่างน้อย 1 รายการ")]
    public List<PurchaseOrderItemCreateDto> Items { get; set; } = new();
}

public class PurchaseOrderItemCreateDto
{
    [Required]
    public int VariantId { get; set; }

    [Required, Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [Required, Range(0, double.MaxValue)]
    public decimal UnitPrice { get; set; }
}

public class PurchaseOrderStatusUpdateDto
{
    [Required]
    public string Status { get; set; } = string.Empty;
    public int? UpdatedBy { get; set; }
}

public class PurchaseOrderSearchDto : PaginationParamsDto
{
    public string? SearchTerm { get; set; }
    public string? Status { get; set; }
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
}

// ─── Goods Receipt ────────────────────────────────────────────────────────────
public class GoodsReceiptListDto
{
    public int ReceiptId { get; set; }
    public string ReceiptNo { get; set; } = string.Empty;
    public string PONo { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public DateTime ReceiptDate { get; set; }
    public string? ShippingCompany { get; set; }
    public string? TrackingNo { get; set; }
    public decimal ShippingCost { get; set; }
    public string Status { get; set; } = string.Empty;
    public int ItemCount { get; set; }
}

public class GoodsReceiptDetailDto
{
    public int ReceiptId { get; set; }
    public string ReceiptNo { get; set; } = string.Empty;
    public int PurchaseOrderId { get; set; }
    public string PONo { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public DateTime ReceiptDate { get; set; }
    public string? ShippingCompany { get; set; }
    public string? TrackingNo { get; set; }
    public decimal ShippingCost { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ShippingPaymentMethod { get; set; }
    public string? ShippingPaymentRefNo { get; set; }
    public string? ShippingSourceAccount { get; set; }
    public string? ShippingSlipUrl { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<GoodsReceiptItemDto> Items { get; set; } = new();
}

public class GoodsReceiptItemDto
{
    public long ReceiptItemId { get; set; }
    public long POItemId { get; set; }
    public int VariantId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string? VariantName { get; set; }
    public string? ImageUrl { get; set; }
    public int ExpectedQuantity { get; set; }
    public int ReceivedQuantity { get; set; }
    public int DefectiveQuantity { get; set; }
    public int DamagedQuantity { get; set; }
}

public class GoodsReceiptCreateDto
{
    [Required, MaxLength(50)]
    public string ReceiptNo { get; set; } = string.Empty;

    [Required]
    public int PurchaseOrderId { get; set; }

    public DateTime ReceiptDate { get; set; } = DateTime.Now;

    [MaxLength(100)]
    public string? ShippingCompany { get; set; }

    [MaxLength(100)]
    public string? TrackingNo { get; set; }

    public decimal ShippingCost { get; set; } = 0;

    // Shipping payment
    [MaxLength(20)]
    public string? ShippingPaymentMethod { get; set; }

    [MaxLength(100)]
    public string? ShippingPaymentRefNo { get; set; }

    [MaxLength(255)]
    public string? ShippingSourceAccount { get; set; }

    [MaxLength(500)]
    public string? ShippingSlipUrl { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public int? CreatedBy { get; set; }

    [Required, MinLength(1)]
    public List<GoodsReceiptItemCreateDto> Items { get; set; } = new();
}

public class GoodsReceiptItemCreateDto
{
    [Required]
    public long POItemId { get; set; }

    [Required]
    public int VariantId { get; set; }

    public int ExpectedQuantity { get; set; }

    [Range(0, int.MaxValue)]
    public int ReceivedQuantity { get; set; }

    [Range(0, int.MaxValue)]
    public int DefectiveQuantity { get; set; }

    [Range(0, int.MaxValue)]
    public int DamagedQuantity { get; set; }
}

public class GoodsReceiptSearchDto : PaginationParamsDto
{
    public string? SearchTerm { get; set; }
    public int? PurchaseOrderId { get; set; }
    public string? Status { get; set; }
}

// ─── Stock Adjustment DTOs ────────────────────────────────────────────────────
public class StockConditionTransferDto
{
    [Required]
    public int VariantId { get; set; }

    [Required, MaxLength(50)]
    public string FromCondition { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string ToCondition { get; set; } = string.Empty;

    [Required, Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [MaxLength(500)]
    public string? Reason { get; set; }

    public int? CreatedBy { get; set; }
}

public class StockScrapDto
{
    [Required]
    public int VariantId { get; set; }

    [Required, MaxLength(50)]
    public string Condition { get; set; } = "Damaged";

    [Required, Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    public bool IsSold { get; set; } = false;

    [Range(0, double.MaxValue)]
    public decimal ScrapPrice { get; set; } = 0;

    [MaxLength(500)]
    public string? Reason { get; set; }

    public int? CreatedBy { get; set; }
}
