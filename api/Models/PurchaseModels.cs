namespace PCS_API.Models;

// ─── Purchase Order ───────────────────────────────────────────────────────────
public class PurchaseOrderModel
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
    public string Status { get; set; } = "DRAFT";
    // DRAFT, ORDERED, PARTIALLY_RECEIVED, RECEIVED, CANCELLED
    public int? TaxInvoiceId { get; set; }
    public DateOnly? ExpectedDeliveryDate { get; set; }

    // Payment details
    public string? PaymentMethod { get; set; }         // CASH, TRANSFER, CREDIT
    public string? PaymentRefNo { get; set; }
    public string? SourceAccountInfo { get; set; }     // บัญชีที่เราโอนออกไป
    public int? ReceiverAccountId { get; set; }
    public string? SlipAttachmentUrl { get; set; }     // ไม่บังคับ

    public string? Notes { get; set; }
    public int? CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class PurchaseOrderItemModel
{
    public long POItemId { get; set; }
    public int PurchaseOrderId { get; set; }
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

// ─── Goods Receipt ────────────────────────────────────────────────────────────
public class GoodsReceiptModel
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
    public string Status { get; set; } = "PENDING";   // PENDING, COMPLETED

    // Shipping Payment Details
    public string? ShippingPaymentMethod { get; set; }
    public string? ShippingPaymentRefNo { get; set; }
    public string? ShippingSourceAccount { get; set; }
    public string? ShippingSlipUrl { get; set; }       // ไม่บังคับ

    public string? Notes { get; set; }
    public int? CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class GoodsReceiptItemModel
{
    public long ReceiptItemId { get; set; }
    public int ReceiptId { get; set; }
    public long POItemId { get; set; }
    public int VariantId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string? VariantName { get; set; }
    public int ExpectedQuantity { get; set; }
    public int ReceivedQuantity { get; set; }
    public int DefectiveQuantity { get; set; }
    public int DamagedQuantity { get; set; }
}
