namespace PCS_API.Models;

public class SupplierModel
{
    public int Id { get; set; }
    public string SupplierCode { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string? ContactName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? TaxId { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class VcbOrderModel
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
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class VcbOrderItemModel
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int VariantId { get; set; }
    public int Quantity { get; set; }
    public decimal TotalPrice { get; set; }
}

public class VcbShipmentModel
{
    public int Id { get; set; }
    public string ReceiptNo { get; set; } = string.Empty;
    public DateTime ReceiptDate { get; set; }
    public int DeliveryId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsForceCloseOrder { get; set; }
}

public class VcbShipmentItemModel
{
    public int Id { get; set; }
    public int ShipmentId { get; set; }
    public int OrderItemId { get; set; }
    public int VariantId { get; set; }
    public string? BoxNumbers { get; set; }
    public string ReceiptStatus { get; set; } = string.Empty;
    public int ExpectedQuantity { get; set; }
    public int GoodQuantity { get; set; }
    public int DefectiveQuantity { get; set; }
    public decimal RefundAmount { get; set; }
}

public class AccountTransactionModel
{
    public int Id { get; set; }
    public DateTime TransactionDate { get; set; }
    public string Type { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? ReferenceType { get; set; }
    public int? ReferenceId { get; set; }
    public string? Notes { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
}
