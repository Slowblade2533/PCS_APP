using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PCS_API.DTOs;
using PCS_API.Services;

namespace PCS_API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "CanViewUsers")]
public class UsersController(IUserService userService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetUsers([FromQuery] UserSearchDto queryParams, CancellationToken cancellationToken)
    {
        var result = await userService.GetUsersAsync(queryParams, cancellationToken);
        return Ok(result);
    }

    [HttpGet("permissions")]
    public async Task<IActionResult> GetPermissions()
    {
        var permissions = await userService.GetPermissionsAsync();
        return Ok(permissions);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetUserById(int id)
    {
        var user = await userService.GetUserByIdAsync(id);
        if (user == null) return NotFound();
        
        return Ok(user);
    }

    [HttpPost]
    [Authorize(Policy = "CanManageUsers")]
    public async Task<IActionResult> CreateUser([FromBody] UserCreateRequestDto request)
    {
        // Typed exceptions (InvalidOperationException, KeyNotFoundException) are handled
        // by GlobalExceptionHandler which maps them to the correct HTTP status codes.
        var user = await userService.CreateUserAsync(request);
        return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, user);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "CanManageUsers")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UserUpdateRequestDto request)
    {
        var user = await userService.UpdateUserAsync(id, request);
        return Ok(user);
    }

    [HttpPatch("{id}/active")]
    [Authorize(Policy = "CanManageUsers")]
    public async Task<IActionResult> ToggleActive(int id, [FromBody] ToggleActiveRequest request)
    {
        await userService.ToggleActiveAsync(id, request.IsActive);
        return NoContent();
    }
}

public class ToggleActiveRequest
{
    public bool IsActive { get; set; }
}
