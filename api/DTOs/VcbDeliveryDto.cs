using System.ComponentModel.DataAnnotations;

namespace PCS_API.DTOs;

public class VcbDeliveryDto
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
    public string? CreatedByUsername { get; set; }
    public int? UpdatedBy { get; set; }
    public string? UpdatedByUsername { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public List<VcbDeliveryOrderDto> Orders { get; set; } = new();
    public List<VcbDeliveryItemDto> Items { get; set; } = new();
    public string? PackageBoxes { get; set; }
    public string? OrderNumbers { get; set; }
    public List<string> ReceivedBoxNumbers { get; set; } = new();
}

public class VcbDeliveryOrderDto
{
    public int DeliveryId { get; set; }
    public int OrderId { get; set; }
    public string? OrderNo { get; set; }
}

public class VcbDeliveryItemDto
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

public class VcbDeliveryCreateDto
{
    [Required]
    public string DeliveryNo { get; set; } = string.Empty;
    [Required]
    public DateTime OrderDate { get; set; }
    [Required]
    public string ShippingAddress { get; set; } = string.Empty;
    [Required]
    public string DomesticShippingCompany { get; set; } = string.Empty;
    public decimal TotalAmountBeforeDiscount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TransferredAmount { get; set; }
    public string? Notes { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "ต้องมีใบสั่งซื้ออย่างน้อย 1 รายการ")]
    public List<int> OrderIds { get; set; } = new();

    [Required]
    [MinLength(1, ErrorMessage = "ต้องมีรายการกล่องอย่างน้อย 1 รายการ")]
    public List<VcbDeliveryItemCreateDto> Items { get; set; } = new();
}

public class VcbDeliveryItemCreateDto
{
    [Required]
    public string PackageBoxNo { get; set; } = string.Empty;
    public string? DomesticTrackingNo { get; set; }
    [Required]
    public decimal TotalWeight { get; set; }
    public string? BoxDimensions { get; set; }
    public string? ContainedBoxNumbers { get; set; }
    public decimal ShippingCost { get; set; }
}

public class VcbDeliverySearchDto : PaginationParamsDto
{
    public string? SearchTerm { get; set; }
    public string? Status { get; set; }
}

public class VcbDeliveryUpdateStatusDto
{
    public string Status { get; set; } = string.Empty;
}
