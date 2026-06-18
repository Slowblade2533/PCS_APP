namespace PCS_API.Models;

public class StockTransactionModel
{
    public long TransactionId { get; set; }
    public int VariantId { get; set; }
    public string TransactionType { get; set; } = string.Empty;
    public string Condition { get; set; } = "Normal";
    public int Quantity { get; set; }
    public decimal? UnitCost { get; set; }
    public string? ReferenceDoc { get; set; }
    public string? Notes { get; set; }
    public DateTime? CreatedAt { get; set; }
    public int? CreatedBy { get; set; }
    public Guid? RequestId { get; set; }

    public int? BranchId { get; set; }
    public int? QuantityBefore { get; set; }
    public int? QuantityAfter { get; set; }
}