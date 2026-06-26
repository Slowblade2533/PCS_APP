using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PCS_API.DTOs;
using PCS_API.Services;

namespace PCS_API.Controllers;

[Route("api/sales-orders")]
[ApiController]
[Authorize]
public class SalesOrdersController(ISalesOrderService salesOrderService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] SalesOrderSearchDto search, CancellationToken ct)
    {
        var result = await salesOrderService.GetPagedAsync(search, ct);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await salesOrderService.GetByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SalesOrderCreateDto dto)
    {
        if (!ModelState.IsValid) 
            return BadRequest(ModelState);

        var result = await salesOrderService.CreateAsync(dto);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpPost("{id}/complete")]
    public async Task<IActionResult> Complete(int id, [FromQuery] int? updatedBy)
    {
        var result = await salesOrderService.CompleteOrderAsync(id, updatedBy);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> Cancel(int id, [FromQuery] int? updatedBy)
    {
        var result = await salesOrderService.CancelOrderAsync(id, updatedBy);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
}
