using PCS_API.DTOs;
using PCS_API.Repositories;
using Microsoft.AspNetCore.Http;

namespace PCS_API.Services;

public class VcbDeliveryService : IVcbDeliveryService
{
    private readonly IVcbDeliveryRepository _repository;
    private readonly IWebHostEnvironment _env;

    public VcbDeliveryService(IVcbDeliveryRepository repository, IWebHostEnvironment env)
    {
        _repository = repository;
        _env = env;
    }

    public async Task<PagedResultDto<VcbDeliveryDto>> GetPagedAsync(VcbDeliverySearchDto search, CancellationToken cancellationToken = default)
    {
        return await _repository.GetPagedAsync(search, cancellationToken);
    }

    public async Task<VcbDeliveryDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _repository.GetByIdAsync(id, cancellationToken);
    }

    public async Task<int> CreateAsync(VcbDeliveryCreateDto dto, IFormFile? slipFile, int currentUserId, CancellationToken cancellationToken = default)
    {
        string? transferSlipUrl = null;

        if (slipFile != null && slipFile.Length > 0)
        {
            // Validate size (max 5MB)
            if (slipFile.Length > 5 * 1024 * 1024)
            {
                throw new ArgumentException("ขนาดไฟล์ต้องไม่เกิน 5MB");
            }

            // Validate extension
            var ext = Path.GetExtension(slipFile.FileName).ToLowerInvariant();
            var allowedExts = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            if (!allowedExts.Contains(ext))
            {
                throw new ArgumentException("รองรับเฉพาะไฟล์รูปภาพ JPG, PNG, WEBP เท่านั้น");
            }

            string uploadDir = Path.Combine(_env.WebRootPath, "uploads", "slips", "deliveries");
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

            transferSlipUrl = $"/uploads/slips/deliveries/{fileName}";
        }

        return await _repository.CreateAsync(dto, transferSlipUrl, currentUserId, cancellationToken);
    }

    public async Task<bool> UpdateAsync(int id, VcbDeliveryCreateDto dto, IFormFile? slipFile, int currentUserId, CancellationToken cancellationToken = default)
    {
        string? transferSlipUrl = null;

        if (slipFile != null && slipFile.Length > 0)
        {
            if (slipFile.Length > 5 * 1024 * 1024)
            {
                throw new ArgumentException("ขนาดไฟล์ต้องไม่เกิน 5MB");
            }

            var ext = Path.GetExtension(slipFile.FileName).ToLowerInvariant();
            var allowedExts = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            if (!allowedExts.Contains(ext))
            {
                throw new ArgumentException("รองรับเฉพาะไฟล์รูปภาพ JPG, PNG, WEBP เท่านั้น");
            }

            string uploadDir = Path.Combine(_env.WebRootPath, "uploads", "slips", "deliveries");
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

            transferSlipUrl = $"/uploads/slips/deliveries/{fileName}";
        }

        return await _repository.UpdateAsync(id, dto, transferSlipUrl, currentUserId, cancellationToken);
    }

    public async Task<bool> UpdateStatusAsync(int id, string status, int currentUserId, CancellationToken cancellationToken = default)
    {
        return await _repository.UpdateStatusAsync(id, status, currentUserId, cancellationToken);
    }
}
