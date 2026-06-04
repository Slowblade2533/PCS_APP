using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PCS_API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UploadController : ControllerBase
{
    private readonly IWebHostEnvironment _env;

    public UploadController(IWebHostEnvironment env)
    {
        _env = env;
    }

    [HttpPost("product-image")]
    public async Task<IActionResult> UploadProductImage(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "ไม่พบรูปภาพ" });

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(ext))
            return BadRequest(new { message = "รองรับเฉพาะไฟล์รูปภาพเท่านั้น" });

        var webRootPath = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var uploadsFolder = Path.Combine(webRootPath, "uploads", "products");
        if (!Directory.Exists(uploadsFolder))
            Directory.CreateDirectory(uploadsFolder);

        // ✨ คำนวณรหัส SHA256 จากไฟล์เพื่อตรวจสอบความซ้ำซ้อน 100%
        string hashString;
        using (var sha256 = System.Security.Cryptography.SHA256.Create())
        {
            using (var memoryStream = new MemoryStream())
            {
                await file.CopyToAsync(memoryStream);
                var hashBytes = sha256.ComputeHash(memoryStream.ToArray());
                hashString = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();

                var uniqueFileName = $"{hashString}{ext}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                var fileUrl = $"/uploads/products/{uniqueFileName}";

                // ⚡ หากไฟล์นี้เคยอัปโหลดมาแล้ว (มี Hash ตรงกัน) ส่ง URL เดิมกลับไปได้เลย ไม่ต้องบันทึกซ้ำ
                if (System.IO.File.Exists(filePath))
                {
                    return Ok(new { imageUrl = fileUrl });
                }

                // หากเป็นไฟล์ใหม่ ให้บันทึกข้อมูลลงดิสก์ตามปกติ
                memoryStream.Position = 0;
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await memoryStream.CopyToAsync(fileStream);
                }

                return Ok(new { imageUrl = fileUrl });
            }
        }
    }
}
