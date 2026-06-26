using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PCS_API.DTOs;
using PCS_API.Services;

namespace PCS_API.Controllers;

[Route("api/goods-receipts")]
[ApiController]
[Authorize]
public class GoodsReceiptsController : ControllerBase
{
    private readonly IGoodsReceiptService _grService;

    public GoodsReceiptsController(IGoodsReceiptService grService)
    {
        _grService = grService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] GoodsReceiptSearchDto search, CancellationToken ct)
    {
        var result = await _grService.GetPagedAsync(search, ct);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _grService.GetByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] GoodsReceiptCreateDto dto)
    {
        if (!ModelState.IsValid) 
            return BadRequest(ModelState);

        var result = await _grService.CreateAsync(dto);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpPost("{id}/complete")]
    public async Task<IActionResult> Complete(int id, [FromQuery] int? updatedBy)
    {
        var result = await _grService.CompleteReceiptAsync(id, updatedBy);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
}
