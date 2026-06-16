using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using PCS_API.DTOs;
using PCS_API.Repositories;
using System.IO;

namespace PCS_API.Services;

public class VcbOrderService : IVcbOrderService
{
    private readonly IVcbOrderRepository _orderRepository;
    private readonly IWebHostEnvironment _env;

    public VcbOrderService(IVcbOrderRepository orderRepository, IWebHostEnvironment env)
    {
        _orderRepository = orderRepository;
        _env = env;
    }

    public async Task<ResultDto<PagedResultDto<VcbOrderDto>>> GetPagedAsync(VcbOrderSearchDto search, CancellationToken cancellationToken = default)
    {
        var result = await _orderRepository.GetPagedAsync(search, cancellationToken);
        return ResultDto<PagedResultDto<VcbOrderDto>>.Success(result);
    }

    public async Task<ResultDto<VcbOrderDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdAsync(id, cancellationToken);
        if (order == null)
            return ResultDto<VcbOrderDto>.Failure("ไม่พบข้อมูลใบสั่งซื้อ VCANBUY");

        return ResultDto<VcbOrderDto>.Success(order);
    }

    public async Task<ResultDto<int>> CreateAsync(VcbOrderCreateDto dto, IFormFile? slipFile, int currentUserId, CancellationToken cancellationToken = default)
    {
        if (dto.Items == null || dto.Items.Count == 0)
            return ResultDto<int>.Failure("กรุณาระบุรายการสินค้าที่สั่งซื้ออย่างน้อย 1 รายการ");

        string? transferSlipUrl = null;

        if (slipFile != null && slipFile.Length > 0)
        {
            if (slipFile.Length > 5 * 1024 * 1024)
                return ResultDto<int>.Failure("ขนาดไฟล์ต้องไม่เกิน 5MB");

            var ext = Path.GetExtension(slipFile.FileName).ToLowerInvariant();
            var allowedExts = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            if (!allowedExts.Contains(ext))
                return ResultDto<int>.Failure("รองรับเฉพาะไฟล์รูปภาพ JPG, PNG, WEBP เท่านั้น");

            string uploadDir = Path.Combine(_env.WebRootPath, "uploads", "slips", "orders");
            if (!Directory.Exists(uploadDir))
            {
                Directory.CreateDirectory(uploadDir);
            }

            string fileName = $"slip_{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid().ToString("N")[..8]}{ext}";
            string filePath = Path.Combine(uploadDir, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await slipFile.CopyToAsync(stream, cancellationToken);
            }

            transferSlipUrl = $"/uploads/slips/orders/{fileName}";
        }

        int newId = await _orderRepository.CreateAsync(dto, transferSlipUrl, currentUserId, cancellationToken);
        return ResultDto<int>.Success(newId);
    }

    public async Task<ResultDto<bool>> UpdateAsync(int id, VcbOrderCreateDto dto, IFormFile? slipFile, int currentUserId, CancellationToken cancellationToken = default)
    {
        if (dto.Items == null || dto.Items.Count == 0)
            return ResultDto<bool>.Failure("กรุณาระบุรายการสินค้าที่สั่งซื้ออย่างน้อย 1 รายการ");

        string? transferSlipUrl = null;

        if (slipFile != null && slipFile.Length > 0)
        {
            if (slipFile.Length > 5 * 1024 * 1024)
                return ResultDto<bool>.Failure("ขนาดไฟล์ต้องไม่เกิน 5MB");

            var ext = Path.GetExtension(slipFile.FileName).ToLowerInvariant();
            var allowedExts = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            if (!allowedExts.Contains(ext))
                return ResultDto<bool>.Failure("รองรับเฉพาะไฟล์รูปภาพ JPG, PNG, WEBP เท่านั้น");

            string uploadDir = Path.Combine(_env.WebRootPath, "uploads", "slips", "orders");
            if (!Directory.Exists(uploadDir))
            {
                Directory.CreateDirectory(uploadDir);
            }

            string fileName = $"slip_{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid().ToString("N")[..8]}{ext}";
            string filePath = Path.Combine(uploadDir, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await slipFile.CopyToAsync(stream, cancellationToken);
            }

            transferSlipUrl = $"/uploads/slips/orders/{fileName}";
        }

        bool updated = await _orderRepository.UpdateAsync(id, dto, transferSlipUrl, currentUserId, cancellationToken);
        if (!updated)
            return ResultDto<bool>.Failure("ไม่สามารถแก้ไขออเดอร์นี้ได้ (ออเดอร์อาจไม่อยู่ในสถานะ 'Pending' หรือไม่พบข้อมูล)");

        return ResultDto<bool>.Success(true);
    }

    public async Task<ResultDto<bool>> UpdateStatusAsync(int id, string status, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdAsync(id, cancellationToken);
        if (order == null)
            return ResultDto<bool>.Failure("ไม่พบข้อมูลใบสั่งซื้อ VCANBUY");

        bool updated = await _orderRepository.UpdateStatusAsync(id, status, cancellationToken);
        if (!updated)
            return ResultDto<bool>.Failure("อัปเดตสถานะไม่สำเร็จ");

        return ResultDto<bool>.Success(true);
    }
}
