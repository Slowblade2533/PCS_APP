using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PCS_API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "CanUploadImage")]
public class UploadController(IWebHostEnvironment env) : ControllerBase
{
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

        var webRootPath = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
        var uploadsFolder = Path.Combine(webRootPath, "uploads", "products");

        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        string hashString;

        using var stream = file.OpenReadStream();
        using (var sha256 = System.Security.Cryptography.SHA256.Create())
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

        var webRootPath = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
        var uploadsFolder = Path.Combine(webRootPath, "uploads", "company-profile");

        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        string hashString;

        using var stream = file.OpenReadStream();
        using (var sha256 = System.Security.Cryptography.SHA256.Create())
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