using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PCS_API.DTOs;
using PCS_API.Repositories;
using PCS_API.Services;
using System.Security.Claims;

namespace PCS_API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class StockController(IStockService stockService, ISqlConnectionFactory connectionFactory, IStockRepository stockRepository) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "CanViewStock")]
    public async Task<IActionResult> Get([FromQuery] StockSearchDto search, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        return Ok(await stockService.GetStockStatusAsync(search, cancellationToken));
    }

    [HttpGet("transactions")]
    [Authorize(Policy = "CanViewStock")]
    public async Task<IActionResult> GetTransactions(
        [FromQuery] string? transactionType,
        [FromQuery] PaginationParamsDto @params,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var result = await stockService.GetTransactionsAsync(transactionType, @params, cancellationToken);
        return Ok(result);
    }

    [HttpPost("transaction")]
    public async Task<IActionResult> Post([FromBody] CreateStockTransactionDto dto, CancellationToken cancellationToken)
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
        var result = await stockService.ProcessStockTransactionAsync(dto, currentUserId, cancellationToken);

        return result
            ? Ok(new { message = "Transaction complete" })
            : BadRequest(new { message = "Transaction failed" });
    }

    /// <summary>
    /// โอนย้ายสต็อกระหว่างสภาพสินค้า (เช่น Normal → Damaged, Normal → Giveaway)
    /// </summary>
    [HttpPost("transfer-condition")]
    [Authorize(Policy = "CanStockAdjust")]
    public async Task<IActionResult> TransferCondition([FromBody] StockConditionTransferDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim != null) dto.CreatedBy = int.Parse(userIdClaim.Value);
        var result = await StockAdjustmentExtensions.TransferConditionAsync(connectionFactory, stockRepository, dto);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// ตัดจำหน่ายสต็อกที่เสียหาย (Scrap หรือ ขายซาก)
    /// </summary>
    [HttpPost("scrap")]
    [Authorize(Policy = "CanStockAdjust")]
    public async Task<IActionResult> Scrap([FromBody] StockScrapDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim != null) dto.CreatedBy = int.Parse(userIdClaim.Value);
        var result = await StockAdjustmentExtensions.ScrapStockAsync(connectionFactory, stockRepository, dto);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
}