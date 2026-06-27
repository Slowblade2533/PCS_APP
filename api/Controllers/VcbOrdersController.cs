using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PCS_API.DTOs;
using PCS_API.Services;
using System.Security.Claims;
using System.Text.Json;

namespace PCS_API.Controllers;

[Route("api/vcb-orders")]
[ApiController]
[Authorize]
public class VcbOrdersController(IVcbOrderService orderService) : ControllerBase
{
    private int GetCurrentUserId()
    {
        return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
    }

    [HttpGet]
    public async Task<IActionResult> GetPaged([FromQuery] VcbOrderSearchDto search, CancellationToken cancellationToken)
    {
        if (!User.HasClaim("permission", "vcb-order:view")) return Forbid();
        var result = await orderService.GetPagedAsync(search, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        if (!User.HasClaim("permission", "vcb-order:view")) return Forbid();
        var result = await orderService.GetByIdAsync(id, cancellationToken);
        if (!result.IsSuccess) 
            return NotFound(result);

        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromForm] IFormCollection form, CancellationToken cancellationToken)
    {
        if (!User.HasClaim("permission", "vcb-order:create")) return Forbid();
        try
        {
            var dataJson = form["data"].FirstOrDefault();
            if (string.IsNullOrEmpty(dataJson))
                return BadRequest(ResultDto<int>.Failure("ข้อมูลไม่ถูกต้อง (Missing data)"));

            var dto = JsonSerializer.Deserialize<VcbOrderCreateDto>(dataJson, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true,
                NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
            });
            if (dto == null) 
                return BadRequest(ResultDto<int>.Failure("ข้อมูลไม่ถูกต้อง (Invalid data format)"));
            if (string.IsNullOrEmpty(dto.OrderNo))
                return BadRequest(ResultDto<int>.Failure("กรุณาระบุเลขที่ออเดอร์"));
            if (dto.Items == null || !dto.Items.Any())
                return BadRequest(ResultDto<int>.Failure("ต้องมีรายการสินค้าอย่างน้อย 1 รายการ"));

            var slipFile = form.Files.GetFile("slipFile");
            int userId = GetCurrentUserId();
            var result = await orderService.CreateAsync(dto, slipFile, userId, cancellationToken);
            if (!result.IsSuccess) 
                return BadRequest(result);

            return Ok(result);
        }
        catch (JsonException)
        {
            return BadRequest(ResultDto<int>.Failure("รูปแบบข้อมูล JSON ไม่ถูกต้อง"));
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromForm] IFormCollection form, CancellationToken cancellationToken)
    {
        if (!User.HasClaim("permission", "vcb-order:edit")) return Forbid();
        try
        {
            var dataJson = form["data"].FirstOrDefault();
            if (string.IsNullOrEmpty(dataJson))
                return BadRequest(ResultDto<bool>.Failure("ข้อมูลไม่ถูกต้อง (Missing data)"));

            var dto = JsonSerializer.Deserialize<VcbOrderCreateDto>(dataJson, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true,
                NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
            });
            if (dto == null) 
                return BadRequest(ResultDto<bool>.Failure("ข้อมูลไม่ถูกต้อง (Invalid data format)"));
            if (string.IsNullOrEmpty(dto.OrderNo))
                return BadRequest(ResultDto<bool>.Failure("กรุณาระบุเลขที่ออเดอร์"));
            if (dto.Items == null || !dto.Items.Any())
                return BadRequest(ResultDto<bool>.Failure("ต้องมีรายการสินค้าอย่างน้อย 1 รายการ"));

            var slipFile = form.Files.GetFile("slipFile");
            int userId = GetCurrentUserId();
            var result = await orderService.UpdateAsync(id, dto, slipFile, userId, cancellationToken);
            if (!result.IsSuccess) 
                return BadRequest(result);

            return Ok(result);
        }
        catch (JsonException)
        {
            return BadRequest(ResultDto<bool>.Failure("รูปแบบข้อมูล JSON ไม่ถูกต้อง"));
        }
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] string status, CancellationToken cancellationToken)
    {
        if (!User.HasClaim("permission", "vcb-order:edit")) return Forbid();
        if (string.IsNullOrEmpty(status)) 
            return BadRequest("กรุณาระบุสถานะ");

        int userId = GetCurrentUserId();
        var result = await orderService.UpdateStatusAsync(id, status, userId, cancellationToken);
        if (!result.IsSuccess) 
            return BadRequest(result);

        return Ok(result);
    }
}
