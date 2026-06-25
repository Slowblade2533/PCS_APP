using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;

namespace PCS_API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "CanUploadImage")]
public class UploadController(IWebHostEnvironment env) : ControllerBase
{
    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB
    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];

    /// <summary>
    /// Validates an image file by inspecting its magic bytes (file signature),
    /// not just the extension. This prevents malicious files renamed to image extensions.
    /// </summary>
    private static bool IsValidImageFile(Stream stream)
    {
        Span<byte> buffer = stackalloc byte[12];
        int bytesRead = stream.Read(buffer);
        stream.Position = 0; // reset for subsequent reads

        if (bytesRead < 4) return false;

        // JPEG: FF D8 FF
        if (buffer[0] == 0xFF && buffer[1] == 0xD8 && buffer[2] == 0xFF)
            return true;

        // PNG: 89 50 4E 47 0D 0A 1A 0A
        if (buffer[0] == 0x89 && buffer[1] == 0x50 && buffer[2] == 0x4E && buffer[3] == 0x47)
            return true;

        // WebP: RIFF????WEBP (bytes 0-3 = RIFF, bytes 8-11 = WEBP)
        if (buffer[0] == 0x52 && buffer[1] == 0x49 && buffer[2] == 0x46 && buffer[3] == 0x46)
        {
            if (bytesRead >= 12 && buffer[8] == 0x57 && buffer[9] == 0x45 && buffer[10] == 0x42 && buffer[11] == 0x50)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Validates, hashes (SHA-256 for deduplication), and saves an uploaded image file.
    /// Returns the relative URL path on success, or a failure message on validation error.
    /// </summary>
    private async Task<(bool IsSuccess, string Result)> SaveImageAsync(IFormFile file, string subFolder)
    {
        if (file == null || file.Length == 0)
            return (false, "ไม่พบรูปภาพ");

        if (file.Length > MaxFileSizeBytes)
            return (false, "ขนาดไฟล์ต้องไม่เกิน 5MB");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            return (false, "รองรับเฉพาะไฟล์รูปภาพ JPG, PNG, WEBP เท่านั้น");

        // Open once and reuse the stream for both magic-byte check and hashing
        await using var stream = file.OpenReadStream();

        if (!IsValidImageFile(stream))
            return (false, "ไฟล์รูปภาพไม่ถูกต้องหรือข้อมูลของรูปภาพมีความเสียหาย");

        // Hash the content for content-addressable storage (automatic deduplication)
        var hashBytes = await SHA256.HashDataAsync(stream);
        var hashString = Convert.ToHexString(hashBytes).ToLowerInvariant();

        var webRootPath = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
        var uploadsFolder = Path.Combine(webRootPath, "uploads", subFolder);
        Directory.CreateDirectory(uploadsFolder); // no-op if already exists

        var uniqueFileName = $"{hashString}{ext}";
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);
        var fileUrl = $"/uploads/{subFolder}/{uniqueFileName}";

        // Deduplication: if the exact same image was already uploaded, return its URL immediately
        if (System.IO.File.Exists(filePath))
            return (true, fileUrl);

        stream.Position = 0;
        await using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 81920, useAsync: true);
        await stream.CopyToAsync(fileStream);

        return (true, fileUrl);
    }

    [HttpPost("product-image")]
    [RequestSizeLimit(6 * 1024 * 1024)] // 6 MB hard limit (slightly above 5 MB to give meaningful error)
    public async Task<IActionResult> UploadProductImage(IFormFile file)
    {
        var (isSuccess, result) = await SaveImageAsync(file, "products");
        if (!isSuccess) return BadRequest(new { message = result });
        return Ok(new { imageUrl = result });
    }

    [HttpPost("company-logo")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> UploadCompanyLogo(IFormFile file)
    {
        var (isSuccess, result) = await SaveImageAsync(file, "company-profile");
        if (!isSuccess) return BadRequest(new { message = result });
        return Ok(new { imageUrl = result });
    }

    private static readonly string[] AllowedAttachmentExtensions = [".jpg", ".jpeg", ".png", ".webp", ".pdf"];

    private static bool IsValidAttachmentFile(Stream stream, string ext)
    {
        Span<byte> buffer = stackalloc byte[12];
        int bytesRead = stream.Read(buffer);
        stream.Position = 0; // reset for subsequent reads

        if (bytesRead < 4) return false;

        if (ext == ".pdf")
        {
            // PDF: %PDF (hex: 25 50 44 46)
            if (buffer[0] == 0x25 && buffer[1] == 0x50 && buffer[2] == 0x44 && buffer[3] == 0x46)
                return true;
            return false;
        }

        // JPEG: FF D8 FF
        if (buffer[0] == 0xFF && buffer[1] == 0xD8 && buffer[2] == 0xFF)
            return true;

        // PNG: 89 50 4E 47 0D 0A 1A 0A
        if (buffer[0] == 0x89 && buffer[1] == 0x50 && buffer[2] == 0x4E && buffer[3] == 0x47)
            return true;

        // WebP: RIFF????WEBP (bytes 0-3 = RIFF, bytes 8-11 = WEBP)
        if (buffer[0] == 0x52 && buffer[1] == 0x49 && buffer[2] == 0x46 && buffer[3] == 0x46)
        {
            if (bytesRead >= 12 && buffer[8] == 0x57 && buffer[9] == 0x45 && buffer[10] == 0x42 && buffer[11] == 0x50)
                return true;
        }

        return false;
    }

    private async Task<(bool IsSuccess, string Result)> SaveAttachmentAsync(IFormFile file, string subFolder)
    {
        if (file == null || file.Length == 0)
            return (false, "ไม่พบไฟล์แนบ");

        if (file.Length > MaxFileSizeBytes)
            return (false, "ขนาดไฟล์ต้องไม่เกิน 5MB");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedAttachmentExtensions.Contains(ext))
            return (false, "รองรับเฉพาะไฟล์รูปภาพ (JPG, PNG, WEBP) หรือไฟล์ PDF เท่านั้น");

        await using var stream = file.OpenReadStream();

        if (!IsValidAttachmentFile(stream, ext))
            return (false, "รูปแบบไฟล์ไม่ถูกต้องหรือข้อมูลไฟล์มีความเสียหาย");

        var hashBytes = await SHA256.HashDataAsync(stream);
        var hashString = Convert.ToHexString(hashBytes).ToLowerInvariant();

        var webRootPath = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
        var uploadsFolder = Path.Combine(webRootPath, "uploads", subFolder);
        Directory.CreateDirectory(uploadsFolder);

        var uniqueFileName = $"{hashString}{ext}";
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);
        var fileUrl = $"/uploads/{subFolder}/{uniqueFileName}";

        if (System.IO.File.Exists(filePath))
            return (true, fileUrl);

        stream.Position = 0;
        await using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 81920, useAsync: true);
        await stream.CopyToAsync(fileStream);

        return (true, fileUrl);
    }

    [HttpPost("transaction-attachment")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> UploadTransactionAttachment(IFormFile file)
    {
        var (isSuccess, result) = await SaveAttachmentAsync(file, "temp");
        if (!isSuccess) return BadRequest(new { message = result });
        return Ok(new { imageUrl = result });
    }
}