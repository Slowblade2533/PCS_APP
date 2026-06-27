using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PCS_API.DTOs;
using PCS_API.Services;
using System.Text.Json;
using System.Security.Claims;

namespace PCS_API.Controllers;

[Route("api/vcb-deliveries")]
[ApiController]
[Authorize]
public class VcbDeliveriesController(IVcbDeliveryService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetPaged([FromQuery] VcbDeliverySearchDto search, CancellationToken cancellationToken)
    {
        if (!User.HasClaim("permission", "vcb-delivery:view")) return Forbid();
        var result = await service.GetPagedAsync(search, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        if (!User.HasClaim("permission", "vcb-delivery:view")) return Forbid();
        var delivery = await service.GetByIdAsync(id, cancellationToken);
        if (delivery == null)
            return NotFound(new { error = "ไม่พบข้อมูลใบสั่งส่งของ" });

        return Ok(delivery);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromForm] IFormCollection form, CancellationToken cancellationToken)
    {
        if (!User.HasClaim("permission", "vcb-delivery:create")) return Forbid();
        try
        {
            var dataJson = form["data"].FirstOrDefault();
            if (string.IsNullOrEmpty(dataJson))
                return BadRequest(new { error = "ข้อมูลไม่ถูกต้อง (Missing data)" });

            var dto = JsonSerializer.Deserialize<VcbDeliveryCreateDto>(dataJson, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true,
                NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
            });
            if (dto == null) 
                return BadRequest(new { error = "ข้อมูลไม่ถูกต้อง (Invalid data format)" });

            var slipFile = form.Files.GetFile("slipFile");

            int currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            int id = await service.CreateAsync(dto, slipFile, currentUserId, cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id }, new { message = "สร้างใบสั่งส่งของสำเร็จ", id });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromForm] IFormCollection form, CancellationToken cancellationToken)
    {
        if (!User.HasClaim("permission", "vcb-delivery:edit")) return Forbid();
        try
        {
            var dataJson = form["data"].FirstOrDefault();
            if (string.IsNullOrEmpty(dataJson))
                return BadRequest(new { error = "ข้อมูลไม่ถูกต้อง (Missing data)" });

            var dto = JsonSerializer.Deserialize<VcbDeliveryCreateDto>(dataJson, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true,
                NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
            });
            if (dto == null) 
                return BadRequest(new { error = "ข้อมูลไม่ถูกต้อง (Invalid data format)" });

            var slipFile = form.Files.GetFile("slipFile");

            int currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            var result = await service.UpdateAsync(id, dto, slipFile, currentUserId, cancellationToken);
            if (!result.IsSuccess) 
                return NotFound(new { error = result.ErrorMessage ?? "ไม่พบข้อมูลใบสั่งส่งของ" });

            return Ok(new { message = "แก้ไขใบสั่งส่งของสำเร็จ", id });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] VcbDeliveryUpdateStatusDto req, CancellationToken cancellationToken)
    {
        if (!User.HasClaim("permission", "vcb-delivery:edit")) return Forbid();
        if (string.IsNullOrEmpty(req.Status)) 
            return BadRequest(new { error = "สถานะไม่ถูกต้อง" });

        int currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
        var result = await service.UpdateStatusAsync(id, req.Status, currentUserId, cancellationToken);
        
        if (!result.IsSuccess) 
            return NotFound(new { error = result.ErrorMessage ?? "ไม่พบข้อมูลใบสั่งส่งของ" });

        return Ok(new { message = "อัปเดตสถานะสำเร็จ" });
    }
}
