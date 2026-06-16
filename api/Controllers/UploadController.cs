using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace PCS_API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "CanUploadImage")]
public class UploadController(IWebHostEnvironment env) : ControllerBase
{
    private static bool IsValidImageFile(Stream stream)
    {
        var buffer = new byte[12];
        int bytesRead = stream.Read(buffer, 0, 12);
        stream.Position = 0; // reset position

        if (bytesRead < 4)
        {
            return false;
        }

        // JPEG: FF D8 FF
        if (buffer[0] == 0xFF && buffer[1] == 0xD8 && buffer[2] == 0xFF)
        {
            return true;
        }

        // PNG: 89 50 4E 47 0D 0A 1A 0A
        if (buffer[0] == 0x89 && buffer[1] == 0x50 && buffer[2] == 0x4E && buffer[3] == 0x47)
        {
            return true;
        }

        // WebP: RIFF ... WEBP (offset 8)
        if (buffer[0] == 0x52 && buffer[1] == 0x49 && buffer[2] == 0x46 && buffer[3] == 0x46)
        {
            if (bytesRead >= 12 && buffer[8] == 0x57 && buffer[9] == 0x45 && buffer[10] == 0x42 && buffer[11] == 0x50)
            {
                return true;
            }
        }

        return false;
    }

    [HttpPost("product-image")]
    public async Task<IActionResult> UploadProductImage(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "ไม่พบรูปภาพ" });
        }

        if (file.Length > 5 * 1024 * 1024)
        {
            return BadRequest(new { message = "ขนาดไฟล์ต้องไม่เกิน 5MB" });
        }

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (!allowedExtensions.Contains(ext))
        {
            return BadRequest(new { message = "รองรับเฉพาะไฟล์รูปภาพเท่านั้น" });
        }

        using var stream = file.OpenReadStream();
        if (!IsValidImageFile(stream))
        {
            return BadRequest(new { message = "ไฟล์รูปภาพไม่ถูกต้องหรือข้อมูลของรูปภาพมีความเสียหาย" });
        }

        var webRootPath = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
        var uploadsFolder = Path.Combine(webRootPath, "uploads", "products");

        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        string hashString;

        using (var sha256 = SHA256.Create())
        {
            var hashBytes = await sha256.ComputeHashAsync(stream);
            hashString = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }

        var uniqueFileName = $"{hashString}{ext}";
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);
        var fileUrl = $"/uploads/products/{uniqueFileName}";

        if (System.IO.File.Exists(filePath))
        {
            return Ok(new { imageUrl = fileUrl });
        }

        stream.Position = 0;
        using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            await stream.CopyToAsync(fileStream);
        }

        return Ok(new { imageUrl = fileUrl });
    }

    [HttpPost("company-logo")]
    public async Task<IActionResult> UploadCompanyLogo(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "ไม่พบรูปภาพ" });
        }

        if (file.Length > 5 * 1024 * 1024)
        {
            return BadRequest(new { message = "ขนาดไฟล์ต้องไม่เกิน 5MB" });
        }

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (!allowedExtensions.Contains(ext))
        {
            return BadRequest(new { message = "รองรับเฉพาะไฟล์รูปภาพเท่านั้น" });
        }

        using var stream = file.OpenReadStream();
        if (!IsValidImageFile(stream))
        {
            return BadRequest(new { message = "ไฟล์รูปภาพไม่ถูกต้องหรือข้อมูลของรูปภาพมีความเสียหาย" });
        }

        var webRootPath = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
        var uploadsFolder = Path.Combine(webRootPath, "uploads", "company-profile");

        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        string hashString;

        using (var sha256 = SHA256.Create())
        {
            var hashBytes = await sha256.ComputeHashAsync(stream);
            hashString = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }

        var uniqueFileName = $"{hashString}{ext}";
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);
        var fileUrl = $"/uploads/company-profile/{uniqueFileName}";

        if (System.IO.File.Exists(filePath))
        {
            return Ok(new { imageUrl = fileUrl });
        }

        stream.Position = 0;
        using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            await stream.CopyToAsync(fileStream);
        }

        return Ok(new { imageUrl = fileUrl });
    }
}