namespace PCS_API.Models;

// ─── Sales Order ──────────────────────────────────────────────────────────────
public class SalesOrderModel
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
    public string OrderStatus { get; set; } = "DRAFT"; // DRAFT, COMPLETED, CANCELLED
    public int? TaxInvoiceId { get; set; }

    // Payment details
    public string? PaymentMethod { get; set; }        // CASH, TRANSFER, CREDIT
    public string? PaymentRefNo { get; set; }
    public int? ReceiverAccountId { get; set; }
    public string? SlipAttachmentUrl { get; set; }    // ไม่บังคับ

    public string? Notes { get; set; }
    public int? CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class SalesOrderItemModel
{
    public long OrderItemId { get; set; }
    public int OrderId { get; set; }
    public int VariantId { get; set; }
    public string Condition { get; set; } = "Normal";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Discount { get; set; }
    public decimal LineTotal { get; set; }
    public decimal UnitCostAtSale { get; set; }
}
