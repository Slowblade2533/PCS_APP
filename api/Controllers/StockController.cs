using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PCS_API.DTOs;
using PCS_API.Services;
using System.Security.Claims;

namespace PCS_API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class StockController(IStockService stockService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "CanViewStock")]
    public async Task<IActionResult> Get([FromQuery] StockSearchDto search)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        return Ok(await stockService.GetStockStatusAsync(search));
    }

    [HttpGet("transactions")]
    [Authorize(Policy = "CanViewStock")] // ปรับสิทธิ์สอดคล้องตามที่ระบบคุณต้องการล็อกความปลอดภัยไว้
    public async Task<IActionResult> GetTransactions(
        [FromQuery] string? transactionType,
        [FromQuery] PaginationParamsDto @params)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var result = await stockService.GetTransactionsAsync(transactionType, @params);
        return Ok(result);
    }

    [HttpPost("transaction")]
    public async Task<IActionResult> Post([FromBody] CreateStockTransactionDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var requiredPermission = dto.TransactionType.ToUpper() switch
        {
            "IN" => "stock:in",
            "OUT" => "stock:out",
            "ADJUST" => "stock:adjust",
            _ => throw new ArgumentException("Invalid TransactionType")
        };

        if (!User.HasClaim("permission", requiredPermission))
        {
            return Forbid();
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

        if (userIdClaim == null)
        {
            return Unauthorized(new
            {
                message = "User not authenticated"
            });
        }

        int currentUserId = int.Parse(userIdClaim.Value);
        var result = await stockService.ProcessStockTransactionAsync(dto, currentUserId);

        return result
            ? Ok(new { message = "Transaction complete" })
            : BadRequest(new { message = "Transaction failed" });
    }
}