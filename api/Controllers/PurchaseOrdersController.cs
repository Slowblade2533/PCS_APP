using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PCS_API.DTOs;
using PCS_API.Services;

namespace PCS_API.Controllers;

[Route("api/purchase-orders")]
[ApiController]
[Authorize]
public class PurchaseOrdersController(IPurchaseOrderService poService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] PurchaseOrderSearchDto search, CancellationToken ct)
    {
        var result = await poService.GetPagedAsync(search, ct);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await poService.GetByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PurchaseOrderCreateDto dto)
    {
        if (!ModelState.IsValid) 
            return BadRequest(ModelState);

        var result = await poService.CreateAsync(dto);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] PurchaseOrderStatusUpdateDto dto)
    {
        var result = await poService.UpdateStatusAsync(id, dto);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpPatch("{id}/slip")]
    public async Task<IActionResult> UpdateSlip(int id, [FromBody] UpdateSlipRequest req)
    {
        var result = await poService.UpdateSlipAsync(id, req.SlipUrl, req.UpdatedBy);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
}

public record UpdateSlipRequest(string SlipUrl, int? UpdatedBy);