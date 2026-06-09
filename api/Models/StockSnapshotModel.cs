namespace PCS_API.Models;

public class StockSnapshotModel
{
    public int CurrentQuantity { get; set; }
    public int ReservedQuantity { get; set; }
    public byte[] RowVersion { get; set; } = [];
}