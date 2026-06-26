using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PCS_API.DTOs;
using PCS_API.Services;

namespace PCS_API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]

public class SuppliersController(ISupplierService supplierService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetPaged([FromQuery] SupplierSearchDto search, CancellationToken cancellationToken)
    {
        var result = await supplierService.GetPagedAsync(search, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await supplierService.GetByIdAsync(id, cancellationToken);
        if (!result.IsSuccess) 
            return NotFound(result);

        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SupplierCreateDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) 
            return BadRequest(ModelState);

        var result = await supplierService.CreateAsync(dto, cancellationToken);
        if (!result.IsSuccess) 
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] SupplierCreateDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) 
            return BadRequest(ModelState);

        var result = await supplierService.UpdateAsync(id, dto, cancellationToken);
        if (!result.IsSuccess) 
            return BadRequest(result);

        return Ok(result);
    }
}
