using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PCS_API.DTOs;
using PCS_API.Services;
using System.Security.Claims;
using System.Security.Cryptography;

namespace PCS_API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class AuthController(IAuthService authService) : ControllerBase
{
    private void GenerateXsrfCookie()
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        
        Response.Cookies.Append(
            "XSRF-TOKEN",
            token,
            new CookieOptions
            {
                HttpOnly = false,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTimeOffset.UtcNow.AddDays(7)
            });
    }

    [AllowAnonymous]
    [HttpGet("csrf-token")]
    public IActionResult GetCsrfToken()
    {
        GenerateXsrfCookie();

        return NoContent();
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [EnableRateLimiting("LoginLimiter")]
    public async Task<IActionResult> Login([FromBody] LoginDto model)
    {
        var user = await authService.AuthenticateAsync(model.Email, model.Password);

        if (user == null)
            return BadRequest(new { message = "อีเมลหรือรหัสผ่านไม่ถูกต้อง" });

        var permissions = await authService.GetUserPermissionsAsync(user.Id);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.FullName)
        };

        foreach (var role in permissions.Select(p => p.RoleName).Distinct())
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }
            
        foreach (var p in permissions)
        {
            if (!claims.Any(c => c.Type == "permission" && c.Value == p.PermissionCode))
                claims.Add(new Claim("permission", p.PermissionCode));
        }

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

        GenerateXsrfCookie();

        return Ok(new
        {
            id = user.Id,
            email = user.Email,
            name = user.FullName,
            roles = permissions.Select(p => p.RoleName).Distinct(),
            permissions = permissions.Select(p => p.PermissionCode).Distinct()
        });
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        // SignOutAsync will correctly delete the AuthCookie using the registered CookieOptions
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        // Delete XSRF-TOKEN with the exact same options used to create it
        Response.Cookies.Delete("XSRF-TOKEN", new CookieOptions
        {
            HttpOnly = false,
            Secure = true,
            SameSite = SameSiteMode.None
        });

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
            name = User.FindFirst(ClaimTypes.Name)?.Value,
            roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value),
            permissions = User.FindAll("permission").Select(c => c.Value)
        });
    }
}