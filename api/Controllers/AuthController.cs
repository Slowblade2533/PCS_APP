using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PCS_API.DTOs;
using PCS_API.Services;
using System.Security.Claims;

namespace PCS_API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IAuthService authService, IAntiforgery antiforgery) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("csrf-token")]
    public IActionResult GetCsrfToken()
    {
        antiforgery.GetAndStoreTokens(HttpContext);
        return NoContent();
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto model)
    {
        var user = await authService.AuthenticateAsync(model.Email, model.Password);

        if (user == null)
        {
            return BadRequest(new { message = "อีเมลหรือรหัสผ่านไม่ถูกต้อง" });
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.FullName)
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties
        );

        antiforgery.GetAndStoreTokens(HttpContext);

        return Ok(new { id = user.Id, email = user.Email, name = user.FullName });
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);

        Response.Cookies.Delete("AuthCookie");
        Response.Cookies.Delete("XSRF-TOKEN");

        return Ok(new
        {
            message = "ออกจากระบบสำเร็จ"
        });
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult GetCurrentUser()
    {
        return Ok(new
        {
            id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
            email = User.FindFirst(ClaimTypes.Email)?.Value,
            name = User.FindFirst(ClaimTypes.Name)?.Value
        });
    }
}
