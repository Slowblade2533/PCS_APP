using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PCS_API.DTOs;
using PCS_API.Services;

namespace PCS_API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class BranchesController(IBranchService branchService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetBranches()
    {
        var branches = await branchService.GetAllActiveBranchesAsync();
        return Ok(branches);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetBranch(int id)
    {
        var branch = await branchService.GetBranchByIdAsync(id);
        if (branch == null) 
            return NotFound(new { message = "Branch not found" });

        return Ok(branch);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateBranch(int id, [FromBody] BranchUpdateDto dto)
    {
        var result = await branchService.UpdateBranchAsync(id, dto);
        if (!result) 
            return NotFound(new { message = "Branch not found" });
        
        return Ok(new { message = "Branch updated successfully" });
    }
}