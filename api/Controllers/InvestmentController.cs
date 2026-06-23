using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PCS_API.DTOs;
using PCS_API.Models;
using PCS_API.Services;

namespace PCS_API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/investments")]
    public class InvestmentController(
        IInvestmentService investmentService,
        IInvestorService investorService) : ControllerBase
    {
        [HttpGet]
        [Authorize(Policy = "CanViewInvestments")]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var investments = await investmentService.GetAllAsync(cancellationToken);
            var investors = await investorService.GetAllAsync(cancellationToken);
            var investorDict = investors.ToDictionary(i => i.InvestorId);

            var dtos = investments.Select(inv => new
            {
                inv.InvestmentId,
                inv.InvestorId,
                InvestorName = investorDict.TryGetValue(inv.InvestorId, out var investor) ? $"{investor.FirstName} {investor.LastName}" : "Unknown",
                inv.InvestmentType,
                inv.PrincipalAmount,
                inv.Currency,
                inv.InterestRate,
                inv.StartDate,
                inv.MaturityDate,
                inv.Status,
                inv.ContractUrl,
                inv.CompanyBankAccountId,
                inv.IsCash,
                inv.CreatedAt,
                inv.UpdatedAt
            }).ToList();

            return Ok(dtos);
        }

        [HttpGet("{id}")]
        [Authorize(Policy = "CanViewInvestments")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            var inv = await investmentService.GetByIdAsync(id, cancellationToken);
            if (inv == null) return NotFound();

            var investor = await investorService.GetByIdAsync(inv.InvestorId, cancellationToken);
            var schedules = await investmentService.GetSchedulesByInvestmentAsync(id, cancellationToken);
            var interestSchedules = await investmentService.GetInterestSchedulesByInvestmentAsync(id, cancellationToken);

            return Ok(new
            {
                Investment = new
                {
                    inv.InvestmentId,
                    inv.InvestorId,
                    InvestorName = investor != null ? $"{investor.FirstName} {investor.LastName}" : "Unknown",
                    inv.InvestmentType,
                    inv.PrincipalAmount,
                    inv.Currency,
                    inv.InterestRate,
                    inv.StartDate,
                    inv.MaturityDate,
                    inv.Status,
                    inv.ContractUrl,
                    inv.CompanyBankAccountId,
                    inv.IsCash,
                    inv.CreatedAt,
                    inv.UpdatedAt
                },
                Schedules = schedules.Select(s => new InvestmentScheduleDto
                {
                    ScheduleId = s.ScheduleId,
                    InvestmentId = s.InvestmentId,
                    InstallmentNumber = s.InstallmentNumber,
                    DueDate = s.DueDate,
                    PrincipalAmount = s.PrincipalAmount,
                    InterestAmount = s.InterestAmount,
                    PaidAmount = s.PaidAmount,
                    Status = s.Status,
                    PaymentDate = s.PaymentDate,
                    CompanyBankAccountId = s.CompanyBankAccountId,
                    IsCash = s.IsCash,
                    SlipUrl = s.SlipUrl,
                    TransactionId = s.TransactionId,
                    CreatedAt = s.CreatedAt
                }).ToList(),
                InterestSchedules = interestSchedules.Select(ins => new InvestmentInterestScheduleDto
                {
                    ScheduleId = ins.ScheduleId,
                    InvestmentId = ins.InvestmentId,
                    StartMonth = ins.StartMonth,
                    EndMonth = ins.EndMonth,
                    InterestRate = ins.InterestRate
                }).ToList()
            });
        }

        [HttpPost]
        [Authorize(Policy = "CanCreateInvestments")]
        public async Task<IActionResult> Create([FromBody] InvestmentCreateDto request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var model = new InvestmentModel
            {
                InvestorId = request.InvestorId,
                InvestmentType = request.InvestmentType,
                PrincipalAmount = request.PrincipalAmount,
                Currency = request.Currency,
                InterestRate = request.InterestRate,
                StartDate = request.StartDate,
                MaturityDate = request.MaturityDate,
                ContractUrl = request.ContractUrl,
                CompanyBankAccountId = request.CompanyBankAccountId,
                IsCash = request.IsCash
            };

            var schedules = request.Schedules.Select(s => new InvestmentScheduleModel
            {
                InstallmentNumber = s.InstallmentNumber,
                DueDate = s.DueDate,
                PrincipalAmount = s.PrincipalAmount,
                InterestAmount = s.InterestAmount,
                Status = 0 // Pending
            }).ToList();

            var interestSchedules = request.InterestSchedules.Select(ins => new InvestmentInterestScheduleModel
            {
                StartMonth = ins.StartMonth,
                EndMonth = ins.EndMonth,
                InterestRate = ins.InterestRate
            }).ToList();

            try
            {
                var newId = await investmentService.CreateAsync(model, schedules, interestSchedules, cancellationToken);
                return Ok(new { InvestmentId = newId });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "CanEditInvestments")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            var deleted = await investmentService.DeleteAsync(id, cancellationToken);
            if (!deleted) return NotFound();
            return Ok(new { Success = true });
        }

        [HttpPost("{id}/installments/{scheduleId}/pay")]
        [Authorize(Policy = "CanEditInvestments")]
        public async Task<IActionResult> PayInstallment(Guid id, Guid scheduleId, [FromBody] InvestmentRepaymentDto request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var success = await investmentService.RecordRepaymentAsync(
                scheduleId, 
                request.PaidAmount, 
                request.CompanyBankAccountId, 
                request.IsCash, 
                request.SlipUrl, 
                cancellationToken);

            if (!success)
            {
                return BadRequest(new { Message = "Failed to record repayment. The schedule may be paid already or not found." });
            }

            return Ok(new { Success = true });
        }
    }
}
