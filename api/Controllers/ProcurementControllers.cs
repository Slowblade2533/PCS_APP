using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PCS_API.DTOs;
using PCS_API.Repositories;

namespace PCS_API.Controllers;

[ApiController]
[Route("api/purchase-orders")]
[Authorize]
public class PurchaseOrdersController : ControllerBase
{
    private readonly IPurchaseOrderRepository _poRepo;

    public PurchaseOrdersController(IPurchaseOrderRepository poRepo)
    {
        _poRepo = poRepo;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] PurchaseOrderSearchDto search, CancellationToken ct)
    {
        var result = await _poRepo.GetPagedAsync(search, ct);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _poRepo.GetByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PurchaseOrderCreateDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var result = await _poRepo.CreateAsync(dto);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] PurchaseOrderStatusUpdateDto dto)
    {
        var result = await _poRepo.UpdateStatusAsync(id, dto);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpPatch("{id}/slip")]
    public async Task<IActionResult> UpdateSlip(int id, [FromBody] UpdateSlipRequest req)
    {
        var result = await _poRepo.UpdateSlipAsync(id, req.SlipUrl, req.UpdatedBy);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
}

public record UpdateSlipRequest(string SlipUrl, int? UpdatedBy);

// ─────────────────────────────────────────────────────────────────────────────

[ApiController]
[Route("api/goods-receipts")]
[Authorize]
public class GoodsReceiptsController : ControllerBase
{
    private readonly IGoodsReceiptRepository _grRepo;

    public GoodsReceiptsController(IGoodsReceiptRepository grRepo)
    {
        _grRepo = grRepo;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] GoodsReceiptSearchDto search, CancellationToken ct)
    {
        var result = await _grRepo.GetPagedAsync(search, ct);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _grRepo.GetByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] GoodsReceiptCreateDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var result = await _grRepo.CreateAsync(dto);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpPost("{id}/complete")]
    public async Task<IActionResult> Complete(int id, [FromQuery] int? updatedBy)
    {
        var result = await _grRepo.CompleteReceiptAsync(id, updatedBy);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
}
