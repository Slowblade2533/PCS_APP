using PCS_API.DTOs;
using PCS_API.Repositories;

namespace PCS_API.Services;

public class VcbShipmentService : IVcbShipmentService
{
    private readonly IVcbShipmentRepository _shipmentRepository;

    public VcbShipmentService(IVcbShipmentRepository shipmentRepository)
    {
        _shipmentRepository = shipmentRepository;
    }

    public async Task<ResultDto<PagedResultDto<VcbShipmentDto>>> GetPagedAsync(VcbShipmentSearchDto search, CancellationToken cancellationToken = default)
    {
        var result = await _shipmentRepository.GetPagedAsync(search, cancellationToken);
        return ResultDto<PagedResultDto<VcbShipmentDto>>.Success(result);
    }

    public async Task<ResultDto<VcbShipmentDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var shipment = await _shipmentRepository.GetByIdAsync(id, cancellationToken);
        if (shipment == null)
            return ResultDto<VcbShipmentDto>.Failure("ไม่พบข้อมูลใบรับสินค้า VCANBUY");

        return ResultDto<VcbShipmentDto>.Success(shipment);
    }

    public async Task<ResultDto<int>> CreateAsync(VcbShipmentCreateDto dto, int currentUserId, CancellationToken cancellationToken = default)
    {
        if (dto.Items == null || dto.Items.Count == 0)
            return ResultDto<int>.Failure("กรุณาระบุรายการสินค้าอย่างน้อย 1 รายการ");

        foreach (var item in dto.Items)
        {
            if (item.ReceiptStatus == "Complete" && (item.GoodQuantity + item.DefectiveQuantity) != item.ExpectedQuantity)
                return ResultDto<int>.Failure("สถานะรับครบ จำนวนรวมต้องเท่ากับจำนวนที่สั่ง");
            
            if (item.ReceiptStatus == "Incomplete" && (item.GoodQuantity + item.DefectiveQuantity) >= item.ExpectedQuantity)
                return ResultDto<int>.Failure("สถานะรับไม่ครบ จำนวนรวมต้องน้อยกว่าจำนวนที่สั่ง");

            if (item.ReceiptStatus == "Over" && (item.GoodQuantity + item.DefectiveQuantity) <= item.ExpectedQuantity)
                return ResultDto<int>.Failure("สถานะรับเกิน จำนวนรวมต้องมากกว่าจำนวนที่สั่ง");
        }

        int newId = await _shipmentRepository.CreateAsync(dto, currentUserId, cancellationToken);
        return ResultDto<int>.Success(newId);
    }

    public async Task<ResultDto<bool>> UpdateStatusAsync(int id, string status, int currentUserId, CancellationToken cancellationToken = default)
    {
        var shipment = await _shipmentRepository.GetByIdAsync(id, cancellationToken);
        if (shipment == null)
            return ResultDto<bool>.Failure("ไม่พบข้อมูลใบรับสินค้า VCANBUY");

        bool updated = await _shipmentRepository.UpdateStatusAsync(id, status, currentUserId, cancellationToken);
        if (!updated)
            return ResultDto<bool>.Failure("อัปเดตสถานะไม่สำเร็จ หรือเอกสารไม่ได้อยู่ในสถานะ Draft");

        return ResultDto<bool>.Success(true);
    }

    public async Task<ResultDto<bool>> UpdateAsync(int id, VcbShipmentCreateDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.Items == null || dto.Items.Count == 0)
            return ResultDto<bool>.Failure("กรุณาระบุรายการสินค้าอย่างน้อย 1 รายการ");

        foreach (var item in dto.Items)
        {
            if (item.ReceiptStatus == "Complete" && (item.GoodQuantity + item.DefectiveQuantity) != item.ExpectedQuantity)
                return ResultDto<bool>.Failure("สถานะรับครบ จำนวนรวมต้องเท่ากับจำนวนที่สั่ง");
            
            if (item.ReceiptStatus == "Incomplete" && (item.GoodQuantity + item.DefectiveQuantity) >= item.ExpectedQuantity)
                return ResultDto<bool>.Failure("สถานะรับไม่ครบ จำนวนรวมต้องน้อยกว่าจำนวนที่สั่ง");

            if (item.ReceiptStatus == "Over" && (item.GoodQuantity + item.DefectiveQuantity) <= item.ExpectedQuantity)
                return ResultDto<bool>.Failure("สถานะรับเกิน จำนวนรวมต้องมากกว่าจำนวนที่สั่ง");
        }

        bool updated = await _shipmentRepository.UpdateAsync(id, dto, cancellationToken);
        if (!updated)
            return ResultDto<bool>.Failure("ไม่พบข้อมูลใบรับสินค้า VCANBUY หรือเอกสารไม่ได้อยู่ในสถานะ Draft");

        return ResultDto<bool>.Success(true);
    }
}
