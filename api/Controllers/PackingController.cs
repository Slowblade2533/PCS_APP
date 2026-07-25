using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PCS_API.DTOs;
using PCS_API.Services;
using System.Security.Claims;

namespace PCS_API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class PackingController(IPackingService packingService) : ControllerBase
{
    private int? GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }

    [HttpPost("check-skus")]
    public async Task<IActionResult> CheckSkus([FromBody] List<string> skus, CancellationToken cancellationToken)
    {
        if (skus == null || !skus.Any())
            return Ok(new Dictionary<string, SkuCheckResultDto>());

        var result = await packingService.CheckSkusAsync(skus, cancellationToken);
        return Ok(result);
    }

    [HttpPost("draft-batch")]
    public async Task<IActionResult> CreateDraftBatch([FromBody] CreatePackingBatchDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await packingService.CreateDraftBatchAsync(dto, GetCurrentUserId(), cancellationToken);
        return CreatedAtAction(nameof(GetBatchById), new { id = result.BatchId }, result);
    }

    [HttpGet("batches")]
    public async Task<IActionResult> GetBatches([FromQuery] PaginationParamsDto @params, CancellationToken cancellationToken)
    {
        var result = await packingService.GetBatchesPagedAsync(@params, cancellationToken);
        return Ok(result);
    }

    [HttpGet("batches/{id}")]
    public async Task<IActionResult> GetBatchById(int id, CancellationToken cancellationToken)
    {
        var result = await packingService.GetBatchByIdAsync(id, cancellationToken);
        if (result == null)
            return NotFound(new { message = "ไม่พบข้อมูลรายการแพ๊คที่ระบุ" });

        return Ok(result);
    }

    [HttpPost("confirm-shipment")]
    public async Task<IActionResult> ConfirmShipment([FromBody] ConfirmShipmentDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var success = await packingService.ConfirmShipmentAsync(dto, GetCurrentUserId(), cancellationToken);
        return Ok(new { message = "บันทึกการจัดส่งและตัดสต๊อกสินค้าสำเร็จ", success });
    }

    [HttpGet("backorders")]
    public async Task<IActionResult> GetPendingBackorders(CancellationToken cancellationToken)
    {
        var result = await packingService.GetPendingBackordersAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPost("resolve-backorder")]
    public async Task<IActionResult> ResolveBackorder([FromBody] ResolveBackorderDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var success = await packingService.ResolveBackorderAsync(dto, GetCurrentUserId(), cancellationToken);
        return Ok(new { message = "บันทึกการจัดส่งสินค้าทดแทน/ตามหลังสำเร็จ", success });
    }
}
