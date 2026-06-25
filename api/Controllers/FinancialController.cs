using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using PCS_API.DTOs;
using PCS_API.Repositories;
using PCS_API.Services;

namespace PCS_API.Controllers;

[ApiController]
[Route("api/financial")]
[Authorize]
public class FinancialController : ControllerBase
{
    private readonly IFinancialRepository _financialRepo;
    private readonly IWebHostEnvironment _env;

    public FinancialController(IFinancialRepository financialRepo, IWebHostEnvironment env)
    {
        _financialRepo = financialRepo;
        _env = env;
    }

    // ─── Chart of Accounts ────────────────────────────────────────────────────
    [HttpGet("accounts")]
    [Authorize(Policy = "CanViewFinancials")]
    public async Task<IActionResult> GetAccounts()
    {
        var accounts = await _financialRepo.GetAllAccountsAsync();
        return Ok(accounts);
    }

    [HttpGet("accounts/{id}")]
    [Authorize(Policy = "CanViewFinancials")]
    public async Task<IActionResult> GetAccount(int id)
    {
        var account = await _financialRepo.GetAccountByIdAsync(id);
        return account is null ? NotFound() : Ok(account);
    }

    [HttpPost("accounts")]
    [Authorize(Policy = "CanManageFinancials")]
    public async Task<IActionResult> CreateAccount([FromBody] ChartOfAccountCreateDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var result = await _financialRepo.CreateAccountAsync(dto);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    // ─── Tax Invoices ─────────────────────────────────────────────────────────
    [HttpGet("tax-invoices")]
    [Authorize(Policy = "CanViewFinancials")]
    public async Task<IActionResult> GetTaxInvoices([FromQuery] TaxInvoiceSearchDto search, CancellationToken ct)
    {
        var result = await _financialRepo.GetTaxInvoicesPagedAsync(search, ct);
        return Ok(result);
    }

    [HttpGet("tax-invoices/{id}")]
    [Authorize(Policy = "CanViewFinancials")]
    public async Task<IActionResult> GetTaxInvoice(int id)
    {
        var result = await _financialRepo.GetTaxInvoiceByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("tax-invoices")]
    [Authorize(Policy = "CanManageFinancials")]
    public async Task<IActionResult> CreateTaxInvoice([FromBody] TaxInvoiceCreateDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var result = await _financialRepo.CreateTaxInvoiceAsync(dto);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    // ─── Financial Transactions ────────────────────────────────────────────────
    [HttpGet("transactions")]
    [Authorize(Policy = "CanViewFinancials")]
    public async Task<IActionResult> GetTransactions([FromQuery] FinancialTransactionSearchDto search, CancellationToken ct)
    {
        var result = await _financialRepo.GetTransactionsPagedAsync(search, ct);
        return Ok(result);
    }

    [HttpGet("transactions/{id}")]
    [Authorize(Policy = "CanViewFinancials")]
    public async Task<IActionResult> GetTransaction(int id)
    {
        var result = await _financialRepo.GetTransactionByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("transactions")]
    [Authorize(Policy = "CanManageFinancials")]
    public async Task<IActionResult> CreateTransaction([FromBody] FinancialTransactionCreateDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        dto.AttachmentUrl = AttachmentHelper.CommitAttachment(dto.AttachmentUrl, webRoot, "transactions");

        try
        {
            var result = await _financialRepo.CreateTransactionWithLedgerAsync(dto);
            if (!result.IsSuccess)
            {
                await CleanUpAttachmentAsync(dto.AttachmentUrl);
                return BadRequest(result);
            }
            return Ok(result);
        }
        catch
        {
            await CleanUpAttachmentAsync(dto.AttachmentUrl);
            throw;
        }
    }

    [HttpPut("transactions/{id}")]
    [Authorize(Policy = "CanManageFinancials")]
    public async Task<IActionResult> UpdateTransaction(int id, [FromBody] FinancialTransactionCreateDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        dto.AttachmentUrl = AttachmentHelper.CommitAttachment(dto.AttachmentUrl, webRoot, "transactions");

        try
        {
            var result = await _financialRepo.UpdateTransactionWithLedgerAsync(id, dto);
            if (!result.IsSuccess)
            {
                await CleanUpAttachmentAsync(dto.AttachmentUrl);
                return BadRequest(result);
            }
            return Ok(result);
        }
        catch
        {
            await CleanUpAttachmentAsync(dto.AttachmentUrl);
            throw;
        }
    }

    private async Task CleanUpAttachmentAsync(string? attachmentUrl)
    {
        if (string.IsNullOrWhiteSpace(attachmentUrl)) return;

        try
        {
            bool isUsed = await _financialRepo.IsAttachmentUsedAsync(attachmentUrl);
            if (!isUsed)
            {
                var webRootPath = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
                var fullPath = Path.Combine(webRootPath, attachmentUrl.TrimStart('/'));
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }
            }
        }
        catch
        {
            // Fail silently or log
        }
    }

    // ─── Reports ──────────────────────────────────────────────────────────────
    [HttpGet("reports/trial-balance")]
    [Authorize(Policy = "CanViewReport")]
    public async Task<IActionResult> GetTrialBalance([FromQuery] DateOnly? dateFrom, [FromQuery] DateOnly? dateTo)
    {
        var result = await _financialRepo.GetTrialBalanceAsync(dateFrom, dateTo);
        return Ok(result);
    }

    [HttpGet("reports/general-journal")]
    [Authorize(Policy = "CanViewReport")]
    public async Task<IActionResult> GetGeneralJournal([FromQuery] DateOnly? dateFrom, [FromQuery] DateOnly? dateTo)
    {
        var result = await _financialRepo.GetGeneralJournalAsync(dateFrom, dateTo);
        return Ok(result);
    }

    [HttpGet("reports/general-ledger")]
    [Authorize(Policy = "CanViewReport")]
    public async Task<IActionResult> GetGeneralLedger([FromQuery] int accountId, [FromQuery] DateOnly? dateFrom, [FromQuery] DateOnly? dateTo)
    {
        if (accountId <= 0) return BadRequest("AccountId is required.");
        var result = await _financialRepo.GetGeneralLedgerAsync(accountId, dateFrom, dateTo);
        return Ok(result);
    }

    [HttpGet("reports/profit-loss")]
    [Authorize(Policy = "CanViewReport")]
    public async Task<IActionResult> GetProfitAndLoss([FromQuery] DateOnly? dateFrom, [FromQuery] DateOnly? dateTo)
    {
        var result = await _financialRepo.GetProfitAndLossAsync(dateFrom, dateTo);
        return Ok(result);
    }

    [HttpGet("reports/balance-sheet")]
    [Authorize(Policy = "CanViewReport")]
    public async Task<IActionResult> GetBalanceSheet([FromQuery] DateOnly asOfDate)
    {
        if (asOfDate == default) asOfDate = DateOnly.FromDateTime(DateTime.Today);
        var result = await _financialRepo.GetBalanceSheetAsync(asOfDate);
        return Ok(result);
    }
}
