using PCS_API.DTOs;
using PCS_API.Repositories;
using System.Security.Cryptography;

namespace PCS_API.Services;

public class VcbOrderService(IVcbOrderRepository orderRepository, IWebHostEnvironment env) : IVcbOrderService
{
    private const long MaxSlipFileSizeBytes = 5 * 1024 * 1024; // 5 MB
    private static readonly string[] AllowedSlipExtensions = [".jpg", ".jpeg", ".png", ".webp"];

    public async Task<ResultDto<PagedResultDto<VcbOrderDto>>> GetPagedAsync(VcbOrderSearchDto search, CancellationToken cancellationToken = default)
    {
        var result = await orderRepository.GetPagedAsync(search, cancellationToken);
        return ResultDto<PagedResultDto<VcbOrderDto>>.Success(result);
    }

    public async Task<ResultDto<VcbOrderDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var order = await orderRepository.GetByIdAsync(id, cancellationToken);
        if (order == null)
            return ResultDto<VcbOrderDto>.Failure("ไม่พบข้อมูลใบสั่งซื้อ VCANBUY");

        return ResultDto<VcbOrderDto>.Success(order);
    }

    public async Task<ResultDto<int>> CreateAsync(VcbOrderCreateDto dto, IFormFile? slipFile, int currentUserId, CancellationToken cancellationToken = default)
    {
        if (dto.Items == null || dto.Items.Count == 0)
            return ResultDto<int>.Failure("กรุณาระบุรายการสินค้าที่สั่งซื้ออย่างน้อย 1 รายการ");

        var (slipSaved, slipResult) = await SaveSlipFileAsync(slipFile, "orders", cancellationToken);
        if (!slipSaved && slipResult != null)
            return ResultDto<int>.Failure(slipResult);

        int newId = await orderRepository.CreateAsync(dto, slipResult, currentUserId, cancellationToken);
        return ResultDto<int>.Success(newId);
    }

    public async Task<ResultDto<bool>> UpdateAsync(int id, VcbOrderCreateDto dto, IFormFile? slipFile, int currentUserId, CancellationToken cancellationToken = default)
    {
        if (dto.Items == null || dto.Items.Count == 0)
            return ResultDto<bool>.Failure("กรุณาระบุรายการสินค้าที่สั่งซื้ออย่างน้อย 1 รายการ");

        var (slipSaved, slipResult) = await SaveSlipFileAsync(slipFile, "orders", cancellationToken);
        if (!slipSaved && slipResult != null)
            return ResultDto<bool>.Failure(slipResult);

        bool updated = await orderRepository.UpdateAsync(id, dto, slipResult, currentUserId, cancellationToken);
        if (!updated)
            return ResultDto<bool>.Failure("ไม่สามารถแก้ไขออเดอร์นี้ได้ (ออเดอร์อาจไม่อยู่ในสถานะ 'Pending' หรือไม่พบข้อมูล)");

        return ResultDto<bool>.Success(true);
    }

    public async Task<ResultDto<bool>> UpdateStatusAsync(int id, string status, int currentUserId, CancellationToken cancellationToken = default)
    {
        var order = await orderRepository.GetByIdAsync(id, cancellationToken);
        if (order == null)
            return ResultDto<bool>.Failure("ไม่พบข้อมูลใบสั่งซื้อ VCANBUY");

        bool updated = await orderRepository.UpdateStatusAsync(id, status, currentUserId, null, cancellationToken);
        if (!updated)
            return ResultDto<bool>.Failure("อัปเดตสถานะไม่สำเร็จ");

        return ResultDto<bool>.Success(true);
    }

    /// <summary>
    /// Validates, hashes and persists a payment slip image.
    /// Returns (true, url) on success, (true, null) when no file was provided,
    /// and (false, errorMessage) on validation failure.
    /// </summary>
    private async Task<(bool IsSuccess, string? Result)> SaveSlipFileAsync(
        IFormFile? slipFile, string subFolder, CancellationToken cancellationToken)
    {
        // No file supplied — caller should preserve the existing URL
        if (slipFile == null || slipFile.Length == 0)
            return (true, null);

        if (slipFile.Length > MaxSlipFileSizeBytes)
            return (false, "ขนาดไฟล์ต้องไม่เกิน 5MB");

        var ext = Path.GetExtension(slipFile.FileName).ToLowerInvariant();
        if (!AllowedSlipExtensions.Contains(ext))
            return (false, "รองรับเฉพาะไฟล์รูปภาพ JPG, PNG, WEBP เท่านั้น");

        // Magic-byte validation — prevents renamed malicious files
        await using var stream = slipFile.OpenReadStream();
        if (!IsValidImageFile(stream))
            return (false, "ไฟล์รูปภาพไม่ถูกต้องหรือข้อมูลของรูปภาพมีความเสียหาย");

        // Hash-based filename for deduplication
        var hashBytes = await SHA256.HashDataAsync(stream, cancellationToken);
        var hashString = Convert.ToHexString(hashBytes).ToLowerInvariant();

        var uploadDir = Path.Combine(env.WebRootPath, "uploads", "slips", subFolder);
        Directory.CreateDirectory(uploadDir); // no-op if already exists

        var fileName = $"{hashString}{ext}";
        var filePath = Path.Combine(uploadDir, fileName);
        var fileUrl = $"/uploads/slips/{subFolder}/{fileName}";

        if (File.Exists(filePath))
            return (true, fileUrl);

        stream.Position = 0;
        await using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 81920, useAsync: true);
        await stream.CopyToAsync(fileStream, cancellationToken);

        return (true, fileUrl);
    }

    /// <summary>
    /// Validates an image file's magic bytes (file signature).
    /// Extension-only checks can be bypassed by renaming files.
    /// </summary>
    private static bool IsValidImageFile(Stream stream)
    {
        Span<byte> buffer = stackalloc byte[12];
        int bytesRead = stream.Read(buffer);
        stream.Position = 0;

        if (bytesRead < 4) return false;

        // JPEG: FF D8 FF
        if (buffer[0] == 0xFF && buffer[1] == 0xD8 && buffer[2] == 0xFF)
            return true;

        // PNG: 89 50 4E 47
        if (buffer[0] == 0x89 && buffer[1] == 0x50 && buffer[2] == 0x4E && buffer[3] == 0x47)
            return true;

        // WebP: RIFF????WEBP
        if (buffer[0] == 0x52 && buffer[1] == 0x49 && buffer[2] == 0x46 && buffer[3] == 0x46
            && bytesRead >= 12
            && buffer[8] == 0x57 && buffer[9] == 0x45 && buffer[10] == 0x42 && buffer[11] == 0x50)
            return true;

        return false;
    }
}
