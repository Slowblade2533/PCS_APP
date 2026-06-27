using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PCS_API.DTOs;
using PCS_API.Services;

namespace PCS_API.Controllers;

[Route("api/goods-receipts")]
[ApiController]
[Authorize]
public class GoodsReceiptsController(IGoodsReceiptService grService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] GoodsReceiptSearchDto search, CancellationToken ct)
    {
        var result = await grService.GetPagedAsync(search, ct);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await grService.GetByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] GoodsReceiptCreateDto dto)
    {
        if (!ModelState.IsValid) 
            return BadRequest(ModelState);

        var result = await grService.CreateAsync(dto);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpPost("{id}/complete")]
    public async Task<IActionResult> Complete(int id, [FromQuery] int? updatedBy)
    {
        var result = await grService.CompleteReceiptAsync(id, updatedBy);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
}
