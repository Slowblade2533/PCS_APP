using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PCS_API.DTOs;
using PCS_API.Models;
using PCS_API.Services;

namespace PCS_API.Controllers;

[Route("api/investors")]
[ApiController]
[Authorize]
public class InvestorController(IInvestorService investorService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "CanViewInvestors")]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var investors = await investorService.GetAllAsync(cancellationToken);
        var dtos = new List<InvestorDto>();
        foreach (var inv in investors)
        {
            dtos.Add(new InvestorDto
            {
                InvestorId = inv.InvestorId,
                Title = inv.Title,
                FirstName = inv.FirstName,
                LastName = inv.LastName,
                TaxId = inv.TaxId,
                Address = inv.Address,
                Phone = inv.Phone,
                Email = inv.Email,
                CreatedAt = inv.CreatedAt
            });
        }
        return Ok(dtos);
    }

    [HttpGet("{id}")]
    [Authorize(Policy = "CanViewInvestors")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var inv = await investorService.GetByIdAsync(id, cancellationToken);
        if (inv == null) 
            return NotFound();

        var bankAccounts = await investorService.GetBankAccountsAsync(id, cancellationToken);

        var dto = new InvestorDto
        {
            InvestorId = inv.InvestorId,
            Title = inv.Title,
            FirstName = inv.FirstName,
            LastName = inv.LastName,
            TaxId = inv.TaxId,
            Address = inv.Address,
            Phone = inv.Phone,
            Email = inv.Email,
            CreatedAt = inv.CreatedAt
        };

        // Since InvestorDto in DTOs/InvestorDto.cs didn't have BankAccounts, we can create a sub-class or return a dynamic object.
        // Returning a dynamic object or creating a combined response is clean.
        return Ok(new
        {
            Investor = dto,
            BankAccounts = bankAccounts.Select(b => new InvestorBankAccountDto
            {
                BankAccountId = b.BankAccountId,
                InvestorId = b.InvestorId,
                BankName = b.BankName,
                AccountNumber = b.AccountNumber,
                AccountType = b.AccountType,
                IsDefault = b.IsDefault,
                CreatedAt = b.CreatedAt
            }).ToList()
        });
    }

    [HttpPost]
    [Authorize(Policy = "CanCreateInvestors")]
    public async Task<IActionResult> Create([FromBody] InvestorCreateRequestDto request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) 
            return BadRequest(ModelState);

        var model = new InvestorModel
        {
            Title = request.Title,
            FirstName = request.FirstName,
            LastName = request.LastName,
            TaxId = request.TaxId,
            Address = request.Address,
            Phone = request.Phone,
            Email = request.Email
        };

        var bankAccounts = request.BankAccounts.Select(b => new InvestorBankAccountModel
        {
            BankName = b.BankName,
            AccountNumber = b.AccountNumber,
            AccountType = b.AccountType,
            IsDefault = b.IsDefault
        }).ToList();

        var newId = await investorService.CreateAsync(model, bankAccounts, cancellationToken);
        return Ok(new { InvestorId = newId });
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "CanEditInvestors")]
    public async Task<IActionResult> Update(Guid id, [FromBody] InvestorCreateRequestDto request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) 
            return BadRequest(ModelState);

        var model = new InvestorModel
        {
            InvestorId = id,
            Title = request.Title,
            FirstName = request.FirstName,
            LastName = request.LastName,
            TaxId = request.TaxId,
            Address = request.Address,
            Phone = request.Phone,
            Email = request.Email
        };

        var bankAccounts = request.BankAccounts.Select(b => new InvestorBankAccountModel
        {
            BankAccountId = b.BankAccountId == Guid.Empty ? Guid.NewGuid() : b.BankAccountId,
            InvestorId = id,
            BankName = b.BankName,
            AccountNumber = b.AccountNumber,
            AccountType = b.AccountType,
            IsDefault = b.IsDefault
        }).ToList();

        var updated = await investorService.UpdateAsync(model, bankAccounts, cancellationToken);
        if (!updated) 
            return NotFound();

        return Ok(new { Success = true });
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "CanEditInvestors")] // For deletion, we can reuse CanEditInvestors
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await investorService.DeleteAsync(id, cancellationToken);
        if (!deleted) 
            return NotFound();

        return Ok(new { Success = true });
    }
}

public class InvestorCreateRequestDto
{
    public string Title { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string TaxId { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public List<InvestorBankAccountRequestDto> BankAccounts { get; set; } = new();
}

public class InvestorBankAccountRequestDto
{
    public Guid BankAccountId { get; set; }
    public string BankName { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
}
