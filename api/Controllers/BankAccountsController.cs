using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PCS_API.DTOs;
using PCS_API.Models;
using PCS_API.Repositories;

namespace PCS_API.Controllers;

[ApiController]
[Route("api/bank-accounts")]
[Authorize]
public class BankAccountsController : ControllerBase
{
    private readonly IPartnerBankAccountRepository _partnerRepo;
    private readonly ICompanyBankAccountRepository _companyRepo;

    public BankAccountsController(
        IPartnerBankAccountRepository partnerRepo,
        ICompanyBankAccountRepository companyRepo)
    {
        _partnerRepo = partnerRepo;
        _companyRepo = companyRepo;
    }

    // ─── Partner Bank Accounts ────────────────────────────────────────────────
    [HttpGet("partner")]
    [Authorize(Policy = "CanViewBankAccounts")]
    public async Task<IActionResult> GetPartnerAccounts([FromQuery] string? partnerName, [FromQuery] int? supplierId, CancellationToken ct)
    {
        var accounts = await _partnerRepo.GetByPartnerAsync(partnerName ?? string.Empty, supplierId, ct);
        var dtos = accounts.Select(a => new PartnerBankAccountDto
        {
            Id = a.Id,
            SupplierId = a.SupplierId,
            PartnerName = a.PartnerName,
            BankName = a.BankName,
            AccountNo = a.AccountNo,
            AccountName = a.AccountName,
            IsDefault = a.IsDefault,
            IsActive = a.IsActive,
            CreatedAt = a.CreatedAt
        });
        return Ok(dtos);
    }

    [HttpPost("partner")]
    [Authorize(Policy = "CanManageBankAccounts")]
    public async Task<IActionResult> CreateOrUpdatePartnerAccount([FromBody] PartnerBankAccountCreateDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        bool exists = await _partnerRepo.ExistsAsync(dto.PartnerName, dto.AccountNo, ct);
        if (exists)
        {
            var existingAccounts = await _partnerRepo.GetByPartnerAsync(dto.PartnerName, dto.SupplierId, ct);
            var match = existingAccounts.FirstOrDefault(a => a.AccountNo == dto.AccountNo);
            if (match != null)
            {
                match.SupplierId = dto.SupplierId;
                match.AccountName = dto.AccountName;
                match.BankName = dto.BankName;
                match.IsDefault = dto.IsDefault;
                await _partnerRepo.UpdateAsync(match, ct);
                return Ok(new { value = match.Id });
            }
        }

        var model = new PartnerBankAccountModel
        {
            SupplierId = dto.SupplierId,
            PartnerName = dto.PartnerName,
            BankName = dto.BankName,
            AccountNo = dto.AccountNo,
            AccountName = dto.AccountName,
            IsDefault = dto.IsDefault,
            IsActive = dto.IsActive
        };

        int newId = await _partnerRepo.CreateAsync(model, ct);
        return Ok(new { value = newId });
    }

    [HttpDelete("partner/{id}")]
    [Authorize(Policy = "CanManageBankAccounts")]
    public async Task<IActionResult> DeletePartnerAccount(int id, CancellationToken ct)
    {
        bool success = await _partnerRepo.DeleteAsync(id, ct);
        if (!success) return NotFound();
        return Ok(new { value = success });
    }

    // ─── Company Bank Accounts ────────────────────────────────────────────────
    [HttpGet("company")]
    [Authorize(Policy = "CanViewBankAccounts")]
    public async Task<IActionResult> GetCompanyAccounts([FromQuery] bool activeOnly = false, CancellationToken ct = default)
    {
        var accounts = activeOnly
            ? await _companyRepo.GetAllActiveAsync(ct)
            : await _companyRepo.GetAllAsync(ct);

        var dtos = accounts.Select(a => new CompanyBankAccountDto
        {
            Id = a.Id,
            BankName = a.BankName,
            AccountNo = a.AccountNo,
            AccountName = a.AccountName,
            ChartOfAccountId = a.ChartOfAccountId,
            IsActive = a.IsActive,
            AccountType = a.AccountType,
            CreatedAt = a.CreatedAt
        });
        return Ok(dtos);
    }

    [HttpPost("company")]
    [Authorize(Policy = "CanManageBankAccounts")]
    public async Task<IActionResult> CreateCompanyAccount([FromBody] CompanyBankAccountCreateDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var model = new CompanyBankAccountModel
        {
            BankName = dto.BankName,
            AccountNo = dto.AccountNo,
            AccountName = dto.AccountName,
            ChartOfAccountId = dto.ChartOfAccountId,
            IsActive = dto.IsActive,
            AccountType = dto.AccountType
        };

        int newId = await _companyRepo.CreateAsync(model, ct);
        return Ok(new { value = newId });
    }

    [HttpPut("company/{id}")]
    [Authorize(Policy = "CanManageBankAccounts")]
    public async Task<IActionResult> UpdateCompanyAccount(int id, [FromBody] CompanyBankAccountCreateDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var model = await _companyRepo.GetByIdAsync(id, ct);
        if (model == null) return NotFound();

        model.BankName = dto.BankName;
        model.AccountNo = dto.AccountNo;
        model.AccountName = dto.AccountName;
        model.ChartOfAccountId = dto.ChartOfAccountId;
        model.IsActive = dto.IsActive;
        model.AccountType = dto.AccountType;

        bool success = await _companyRepo.UpdateAsync(model, ct);
        return Ok(new { value = success });
    }

    [HttpDelete("company/{id}")]
    [Authorize(Policy = "CanManageBankAccounts")]
    public async Task<IActionResult> DeleteCompanyAccount(int id, CancellationToken ct)
    {
        bool success = await _companyRepo.DeleteAsync(id, ct);
        if (!success) return NotFound();
        return Ok(new { value = success });
    }
}
