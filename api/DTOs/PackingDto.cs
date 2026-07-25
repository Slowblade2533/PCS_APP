namespace PCS_API.DTOs;

public class CreatePackingBatchDto
{
    public string? BatchNo { get; set; }
    public int TotalFiles { get; set; }
    public int TotalOrders { get; set; }
    public int TotalParcels { get; set; }
    public int TotalItems { get; set; }
    public string? Notes { get; set; }
    public List<PackingBatchItemCreateDto> Items { get; set; } = new();
}

public class PackingBatchItemCreateDto
{
    public string Platform { get; set; } = string.Empty; // Lazada, Shopee
    public string OrderNo { get; set; } = string.Empty;
    public string TrackingNo { get; set; } = string.Empty;
    public string? ShippingProvider { get; set; }
    public string Sku { get; set; } = string.Empty;
    public int? VariantId { get; set; }
    public string? ProductName { get; set; }
    public int OrderedQuantity { get; set; }
    public int ShippedQuantity { get; set; } = 0;
    public DateTime? OrderDate { get; set; }
    public bool IsMultiParcel { get; set; }
    public int ParcelSeq { get; set; } = 1;
    public int TotalParcelsInOrder { get; set; } = 1;
    public bool IsSkuMatched { get; set; } = true;
}

public class PackingBatchResponseDto
{
    public int BatchId { get; set; }
    public string BatchNo { get; set; } = string.Empty;
    public DateTime ImportedAt { get; set; }
    public int? ImportedBy { get; set; }
    public int TotalFiles { get; set; }
    public int TotalOrders { get; set; }
    public int TotalParcels { get; set; }
    public int TotalItems { get; set; }
    public string Status { get; set; } = "DRAFT";
    public string? Notes { get; set; }
    public List<PackingBatchItemResponseDto> Items { get; set; } = new();
}

public class PackingBatchItemResponseDto
{
    public long Id { get; set; }
    public int BatchId { get; set; }
    public string Platform { get; set; } = string.Empty;
    public string OrderNo { get; set; } = string.Empty;
    public string TrackingNo { get; set; } = string.Empty;
    public string? ShippingProvider { get; set; }
    public string Sku { get; set; } = string.Empty;
    public int? VariantId { get; set; }
    public string? ProductName { get; set; }
    public int OrderedQuantity { get; set; }
    public int ShippedQuantity { get; set; }
    public int PendingQuantity => OrderedQuantity - ShippedQuantity;
    public DateTime? OrderDate { get; set; }
    public bool IsMultiParcel { get; set; }
    public int ParcelSeq { get; set; }
    public int TotalParcelsInOrder { get; set; }
    public string PackStatus { get; set; } = "DRAFT";
    public bool IsSkuMatched { get; set; }
}

public class ConfirmShipmentDto
{
    public List<ConfirmShipmentItemDto> Items { get; set; } = new();
}

public class ConfirmShipmentItemDto
{
    public long BatchItemId { get; set; }
    public int ShippedQuantity { get; set; }
}

public class ResolveBackorderDto
{
    public long BatchItemId { get; set; }
    public int ActualShippedVariantId { get; set; }
    public int ShippedQuantity { get; set; }
    public string ResolutionType { get; set; } = "SUBSTITUTION"; // EXACT_FULFILLMENT, SUBSTITUTION, REFUND_CANCEL
    public string? CustomerAgreementNote { get; set; }
    public string? FollowUpTrackingNo { get; set; }
}
