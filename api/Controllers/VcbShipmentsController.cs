using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PCS_API.DTOs;
using PCS_API.Services;
using System.Security.Claims;

namespace PCS_API.Controllers;

[Route("api/vcb-shipments")]
[ApiController]
[Authorize]
public class VcbShipmentsController(IVcbShipmentService shipmentService) : ControllerBase
{
    private int GetCurrentUserId()
    {
        return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
    }

    [HttpGet]
    public async Task<IActionResult> GetPaged([FromQuery] VcbShipmentSearchDto search, CancellationToken cancellationToken)
    {
        if (!User.HasClaim("permission", "vcb-shipment:view")) return Forbid();
        var result = await shipmentService.GetPagedAsync(search, cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        if (!User.HasClaim("permission", "vcb-shipment:view")) return Forbid();
        var result = await shipmentService.GetByIdAsync(id, cancellationToken);
        if (!result.IsSuccess) 
            return NotFound(result);

        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] VcbShipmentCreateDto dto, CancellationToken cancellationToken)
    {
        if (!User.HasClaim("permission", "vcb-shipment:create")) return Forbid();
        if (!ModelState.IsValid) 
            return BadRequest(ModelState);

        int userId = GetCurrentUserId();
        var result = await shipmentService.CreateAsync(dto, userId, cancellationToken);
        if (!result.IsSuccess) 
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] string status, CancellationToken cancellationToken)
    {
        if (!User.HasClaim("permission", "vcb-shipment:edit")) return Forbid();
        if (string.IsNullOrEmpty(status)) 
            return BadRequest("กรุณาระบุสถานะ");

        int userId = GetCurrentUserId();
        var result = await shipmentService.UpdateStatusAsync(id, status, userId, cancellationToken);
        if (!result.IsSuccess) 
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] VcbShipmentCreateDto dto, CancellationToken cancellationToken)
    {
        if (!User.HasClaim("permission", "vcb-shipment:edit")) return Forbid();
        if (!ModelState.IsValid) 
            return BadRequest(ModelState);

        int userId = GetCurrentUserId();
        bool isSuperuser = User.IsInRole("Owner") || User.IsInRole("IT Administrator");

        var result = await shipmentService.UpdateAsync(id, dto, userId, isSuperuser, cancellationToken);
        if (!result.IsSuccess) 
            return BadRequest(result);

        return Ok(result);
    }
}
