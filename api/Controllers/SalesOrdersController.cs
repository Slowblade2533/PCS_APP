using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PCS_API.DTOs;
using PCS_API.Repositories;

namespace PCS_API.Controllers;

[ApiController]
[Route("api/sales-orders")]
[Authorize]
public class SalesOrdersController : ControllerBase
{
    private readonly ISalesOrderRepository _salesOrderRepo;

    public SalesOrdersController(ISalesOrderRepository salesOrderRepo)
    {
        _salesOrderRepo = salesOrderRepo;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] SalesOrderSearchDto search, CancellationToken ct)
    {
        var result = await _salesOrderRepo.GetPagedAsync(search, ct);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _salesOrderRepo.GetByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SalesOrderCreateDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var result = await _salesOrderRepo.CreateAsync(dto);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpPost("{id}/complete")]
    public async Task<IActionResult> Complete(int id, [FromQuery] int? updatedBy)
    {
        var result = await _salesOrderRepo.CompleteOrderAsync(id, updatedBy);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> Cancel(int id, [FromQuery] int? updatedBy)
    {
        var result = await _salesOrderRepo.CancelOrderAsync(id, updatedBy);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
}
