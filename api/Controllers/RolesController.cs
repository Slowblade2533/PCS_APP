using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PCS_API.Services;

namespace PCS_API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "CanViewUsers")]
public class RolesController(IUserService userService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetRoles()
    {
        var roles = await userService.GetRolesAsync();
        return Ok(roles);
    }
}
