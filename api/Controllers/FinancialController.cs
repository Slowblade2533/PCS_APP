using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PCS_API.DTOs;
using PCS_API.Services;

namespace PCS_API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class FinancialController(
        IChartOfAccountService accountService,
        ITaxInvoiceService taxInvoiceService,
        IFinancialTransactionService transactionService,
        IFinancialReportService reportService,
        IWebHostEnvironment env) : ControllerBase
{
    // ─── Chart of Accounts ────────────────────────────────────────────────────
    [HttpGet("accounts")]
    [Authorize(Policy = "CanViewFinancials")]
    public async Task<IActionResult> GetAccounts(CancellationToken ct)
    {
        var accounts = await accountService.GetAllAccountsAsync(ct);
        return Ok(accounts);
    }

    [HttpGet("accounts/{id}")]
    [Authorize(Policy = "CanViewFinancials")]
    public async Task<IActionResult> GetAccount(int id, CancellationToken ct)
    {
        var account = await accountService.GetAccountByIdAsync(id, ct);
        return account is null ? NotFound() : Ok(account);
    }

    [HttpPost("accounts")]
    [Authorize(Policy = "CanManageFinancials")]
    public async Task<IActionResult> CreateAccount([FromBody] ChartOfAccountCreateDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid) 
            return BadRequest(ModelState);

        var result = await accountService.CreateAccountAsync(dto, ct);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    // ─── Tax Invoices ─────────────────────────────────────────────────────────
    [HttpGet("tax-invoices")]
    [Authorize(Policy = "CanViewFinancials")]
    public async Task<IActionResult> GetTaxInvoices([FromQuery] TaxInvoiceSearchDto search, CancellationToken ct)
    {
        var result = await taxInvoiceService.GetTaxInvoicesPagedAsync(search, ct);
        return Ok(result);
    }

    [HttpGet("tax-invoices/{id}")]
    [Authorize(Policy = "CanViewFinancials")]
    public async Task<IActionResult> GetTaxInvoice(int id, CancellationToken ct)
    {
        var result = await taxInvoiceService.GetTaxInvoiceByIdAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("tax-invoices")]
    [Authorize(Policy = "CanManageFinancials")]
    public async Task<IActionResult> CreateTaxInvoice([FromBody] TaxInvoiceCreateDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid) 
            return BadRequest(ModelState);

        var result = await taxInvoiceService.CreateTaxInvoiceAsync(dto, ct);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    // ─── Financial Transactions ────────────────────────────────────────────────
    [HttpGet("transactions")]
    [Authorize(Policy = "CanViewFinancials")]
    public async Task<IActionResult> GetTransactions([FromQuery] FinancialTransactionSearchDto search, CancellationToken ct)
    {
        var result = await transactionService.GetTransactionsPagedAsync(search, ct);
        return Ok(result);
    }

    [HttpGet("transactions/{id}")]
    [Authorize(Policy = "CanViewFinancials")]
    public async Task<IActionResult> GetTransaction(int id, CancellationToken ct)
    {
        var result = await transactionService.GetTransactionByIdAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("transactions")]
    [Authorize(Policy = "CanManageFinancials")]
    public async Task<IActionResult> CreateTransaction([FromBody] FinancialTransactionCreateDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid) 
            return BadRequest(ModelState);

        var webRoot = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
        dto.AttachmentUrl = AttachmentHelper.CommitAttachment(dto.AttachmentUrl, webRoot, "transactions");

        try
        {
            var result = await transactionService.CreateTransactionWithLedgerAsync(dto, null, ct);
            if (!result.IsSuccess)
            {
                await CleanUpAttachmentAsync(dto.AttachmentUrl, ct);
                return BadRequest(result);
            }
            return Ok(result);
        }
        catch
        {
            await CleanUpAttachmentAsync(dto.AttachmentUrl, ct);
            throw;
        }
    }

    [HttpPut("transactions/{id}")]
    [Authorize(Policy = "CanManageFinancials")]
    public async Task<IActionResult> UpdateTransaction(int id, [FromBody] FinancialTransactionCreateDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid) 
            return BadRequest(ModelState);

        var webRoot = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
        dto.AttachmentUrl = AttachmentHelper.CommitAttachment(dto.AttachmentUrl, webRoot, "transactions");

        try
        {
            var result = await transactionService.UpdateTransactionWithLedgerAsync(id, dto, null, ct);
            if (!result.IsSuccess)
            {
                await CleanUpAttachmentAsync(dto.AttachmentUrl, ct);
                return BadRequest(result);
            }
            return Ok(result);
        }
        catch
        {
            await CleanUpAttachmentAsync(dto.AttachmentUrl, ct);
            throw;
        }
    }

    private async Task CleanUpAttachmentAsync(string? attachmentUrl, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(attachmentUrl)) 
            return;

        try
        {
            bool isUsed = await reportService.IsAttachmentUsedAsync(attachmentUrl, ct);
            if (!isUsed)
            {
                var webRootPath = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
                var fullPath = Path.Combine(webRootPath, attachmentUrl.TrimStart('/'));
                if (System.IO.File.Exists(fullPath))
                    System.IO.File.Delete(fullPath);
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
    public async Task<IActionResult> GetTrialBalance([FromQuery] DateOnly? dateFrom, [FromQuery] DateOnly? dateTo, CancellationToken ct)
    {
        var result = await reportService.GetTrialBalanceAsync(dateFrom, dateTo, ct);
        return Ok(result);
    }

    [HttpGet("reports/general-journal")]
    [Authorize(Policy = "CanViewReport")]
    public async Task<IActionResult> GetGeneralJournal([FromQuery] DateOnly? dateFrom, [FromQuery] DateOnly? dateTo, CancellationToken ct)
    {
        var result = await reportService.GetGeneralJournalAsync(dateFrom, dateTo, ct);
        return Ok(result);
    }

    [HttpGet("reports/general-ledger")]
    [Authorize(Policy = "CanViewReport")]
    public async Task<IActionResult> GetGeneralLedger([FromQuery] int accountId, [FromQuery] DateOnly? dateFrom, [FromQuery] DateOnly? dateTo, CancellationToken ct)
    {
        if (accountId <= 0) 
            return BadRequest("AccountId is required.");

        var result = await reportService.GetGeneralLedgerAsync(accountId, dateFrom, dateTo, ct);
        return Ok(result);
    }

    [HttpGet("reports/profit-loss")]
    [Authorize(Policy = "CanViewReport")]
    public async Task<IActionResult> GetProfitAndLoss([FromQuery] DateOnly? dateFrom, [FromQuery] DateOnly? dateTo, CancellationToken ct)
    {
        var result = await reportService.GetProfitAndLossAsync(dateFrom, dateTo, ct);
        return Ok(result);
    }

    [HttpGet("reports/balance-sheet")]
    [Authorize(Policy = "CanViewReport")]
    public async Task<IActionResult> GetBalanceSheet([FromQuery] DateOnly asOfDate, CancellationToken ct)
    {
        if (asOfDate == default) 
            asOfDate = DateOnly.FromDateTime(DateTime.Today);

        var result = await reportService.GetBalanceSheetAsync(asOfDate, ct);
        return Ok(result);
    }
}
