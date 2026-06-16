using System.ComponentModel.DataAnnotations;

namespace PCS_API.Models;

public class VcbDeliveryModel
{
    public int Id { get; set; }
    public string DeliveryNo { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public string ShippingAddress { get; set; } = string.Empty;
    public string DomesticShippingCompany { get; set; } = string.Empty;
    public decimal TotalAmountBeforeDiscount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TransferredAmount { get; set; }
    public string? TransferSlipUrl { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public List<VcbDeliveryOrderModel> Orders { get; set; } = new();
    public List<VcbDeliveryItemModel> Items { get; set; } = new();
}

public class VcbDeliveryOrderModel
{
    public int DeliveryId { get; set; }
    public int OrderId { get; set; }
}

public class VcbDeliveryItemModel
{
    public int Id { get; set; }
    public int DeliveryId { get; set; }
    public string PackageBoxNo { get; set; } = string.Empty;
    public string? DomesticTrackingNo { get; set; }
    public decimal TotalWeight { get; set; }
    public string? BoxDimensions { get; set; }
    public string? ContainedBoxNumbers { get; set; }
    public decimal ShippingCost { get; set; }
}
