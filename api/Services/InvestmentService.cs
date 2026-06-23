using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PCS_API.DTOs;
using PCS_API.Models;
using PCS_API.Repositories;

namespace PCS_API.Services
{
    public class InvestmentService(
        IInvestmentRepository investmentRepo,
        IInvestorRepository investorRepo,
        IFinancialRepository financialRepo,
        ICompanyBankAccountRepository companyBankAccountRepo) : IInvestmentService
    {
        public async Task<IEnumerable<InvestmentModel>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await investmentRepo.GetAllAsync(cancellationToken);
        }

        public async Task<InvestmentModel?> GetByIdAsync(Guid investmentId, CancellationToken cancellationToken = default)
        {
            return await investmentRepo.GetByIdAsync(investmentId, cancellationToken);
        }

        public async Task<Guid> CreateAsync(InvestmentModel model, List<InvestmentScheduleModel> schedules, List<InvestmentInterestScheduleModel> interestSchedules, CancellationToken cancellationToken = default)
        {
            // Verify investor exists
            var investor = await investorRepo.GetByIdAsync(model.InvestorId, cancellationToken);
            if (investor == null)
            {
                throw new ArgumentException("Investor not found.");
            }

            model.InvestmentId = model.InvestmentId == Guid.Empty ? Guid.NewGuid() : model.InvestmentId;
            model.Status = 0; // Active
            model.CreatedAt = DateTime.UtcNow;
            model.UpdatedAt = DateTime.UtcNow;

            var investmentId = await investmentRepo.CreateAsync(model, cancellationToken);

            // Create schedules
            if (schedules != null && schedules.Count > 0)
            {
                foreach (var s in schedules)
                {
                    s.InvestmentId = investmentId;
                }
                await investmentRepo.CreateSchedulesAsync(schedules, cancellationToken);
            }

            // Create interest schedules
            if (interestSchedules != null && interestSchedules.Count > 0)
            {
                foreach (var ins in interestSchedules)
                {
                    ins.InvestmentId = investmentId;
                }
                await investmentRepo.CreateInterestSchedulesAsync(interestSchedules, cancellationToken);
            }

            // Auto-generate accounting journal entry for the capital receipt
            try
            {
                int targetCashBankAccountId = 1; // Default Cash (เงินสด)
                if (!model.IsCash && model.CompanyBankAccountId.HasValue)
                {
                    var bankAcc = await companyBankAccountRepo.GetByIdAsync(model.CompanyBankAccountId.Value, cancellationToken);
                    if (bankAcc != null && bankAcc.ChartOfAccountId.HasValue)
                    {
                        targetCashBankAccountId = bankAcc.ChartOfAccountId.Value;
                    }
                    else
                    {
                        targetCashBankAccountId = 2; // Default Bank (เงินฝากธนาคาร)
                    }
                }

                int creditAccountId = model.InvestmentType == 0 ? 9 : 20; // 9 = ทุนเจ้าของ (3000), 20 = เงินกู้ยืมจากผู้ลงทุน (2100)

                var journalDto = new FinancialTransactionCreateDto
                {
                    TransactionDate = DateOnly.FromDateTime(model.StartDate),
                    TransactionType = "INVESTMENT",
                    Description = $"รับเงินร่วมลงทุน / เงินกู้ยืม จากคุณ {investor.FirstName} {investor.LastName} [Ref: {investmentId}]",
                    TotalAmount = model.PrincipalAmount,
                    PaymentMethod = model.IsCash ? "CASH" : "TRANSFER",
                    AttachmentUrl = model.ContractUrl,
                    Status = "POSTED",
                    LedgerEntries = new List<LedgerEntryCreateDto>
                    {
                        new() { AccountId = targetCashBankAccountId, DebitAmount = model.PrincipalAmount, CreditAmount = 0, Memo = "รับเงินทุนเข้า" },
                        new() { AccountId = creditAccountId, DebitAmount = 0, CreditAmount = model.PrincipalAmount, Memo = "บันทึกบัญชีเจ้าหนี้/ทุนผู้ลงทุน" }
                    }
                };

                await financialRepo.CreateTransactionWithLedgerAsync(journalDto);
            }
            catch (Exception)
            {
                // Log and swallow or handle journal entry failure so that investment creation itself doesn't crash
            }

            return investmentId;
        }

        public async Task<bool> UpdateAsync(InvestmentModel model, CancellationToken cancellationToken = default)
        {
            return await investmentRepo.UpdateAsync(model, cancellationToken);
        }

        public async Task<bool> DeleteAsync(Guid investmentId, CancellationToken cancellationToken = default)
        {
            return await investmentRepo.DeleteAsync(investmentId, cancellationToken);
        }

        public async Task<IEnumerable<InvestmentScheduleModel>> GetSchedulesByInvestmentAsync(Guid investmentId, CancellationToken cancellationToken = default)
        {
            return await investmentRepo.GetSchedulesByInvestmentAsync(investmentId, cancellationToken);
        }

        public async Task<IEnumerable<InvestmentInterestScheduleModel>> GetInterestSchedulesByInvestmentAsync(Guid investmentId, CancellationToken cancellationToken = default)
        {
            return await investmentRepo.GetInterestSchedulesByInvestmentAsync(investmentId, cancellationToken);
        }

        public async Task<bool> RecordRepaymentAsync(Guid scheduleId, decimal paidAmount, int? companyBankAccountId, bool isCash, string? slipUrl, CancellationToken cancellationToken = default)
        {
            var schedule = await investmentRepo.GetScheduleByIdAsync(scheduleId, cancellationToken);
            if (schedule == null || schedule.Status == 1) return false;

            var investment = await investmentRepo.GetByIdAsync(schedule.InvestmentId, cancellationToken);
            if (investment == null) return false;

            var investor = await investorRepo.GetByIdAsync(investment.InvestorId, cancellationToken);
            string investorName = investor != null ? $"{investor.FirstName} {investor.LastName}" : "ผู้ลงทุน";

            int targetCashBankAccountId = 1; // Default Cash (เงินสด)
            if (!isCash && companyBankAccountId.HasValue)
            {
                var bankAcc = await companyBankAccountRepo.GetByIdAsync(companyBankAccountId.Value, cancellationToken);
                if (bankAcc != null && bankAcc.ChartOfAccountId.HasValue)
                {
                    targetCashBankAccountId = bankAcc.ChartOfAccountId.Value;
                }
                else
                {
                    targetCashBankAccountId = 2; // Default Bank (เงินฝากธนาคาร)
                }
            }

            // Create Financial Transaction for Repayment (Debit Loans Payable and Interest Expense, Credit Cash/Bank)
            int? txId = null;
            try
            {
                var journalDto = new FinancialTransactionCreateDto
                {
                    TransactionDate = DateOnly.FromDateTime(DateTime.Today),
                    TransactionType = "EXPENSE",
                    Description = $"จ่ายชำระคืนเงินกู้ยืมงวดที่ {schedule.InstallmentNumber}/{schedule.InstallmentNumber} แด่ {investorName} [Ref: {investment.InvestmentId}]",
                    TotalAmount = paidAmount,
                    PaymentMethod = isCash ? "CASH" : "TRANSFER",
                    AttachmentUrl = slipUrl,
                    Status = "POSTED",
                    LedgerEntries = new List<LedgerEntryCreateDto>()
                };

                // Debit Loans Payable
                if (schedule.PrincipalAmount > 0)
                {
                    journalDto.LedgerEntries.Add(new LedgerEntryCreateDto
                    {
                        AccountId = 20, // 2100: เงินกู้ยืมจากผู้ลงทุน
                        DebitAmount = schedule.PrincipalAmount,
                        CreditAmount = 0,
                        Memo = "ชำระคืนเงินต้น"
                    });
                }

                // Debit Interest Expense
                if (schedule.InterestAmount > 0)
                {
                    journalDto.LedgerEntries.Add(new LedgerEntryCreateDto
                    {
                        AccountId = 21, // 6100: ดอกเบี้ยจ่าย
                        DebitAmount = schedule.InterestAmount,
                        CreditAmount = 0,
                        Memo = "ชำระดอกเบี้ยจ่าย"
                    });
                }

                // Credit Bank/Cash
                journalDto.LedgerEntries.Add(new LedgerEntryCreateDto
                {
                    AccountId = targetCashBankAccountId,
                    DebitAmount = 0,
                    CreditAmount = paidAmount,
                    Memo = "จ่ายออกจากบัญชีธนาคาร/เงินสด"
                });

                var journalResult = await financialRepo.CreateTransactionWithLedgerAsync(journalDto);
                if (journalResult.IsSuccess)
                {
                    txId = journalResult.Value;
                }
            }
            catch (Exception)
            {
                // Swallow or log, we can still proceed to update status
            }

            // Update status in schedule
            var updated = await investmentRepo.UpdateScheduleStatusAsync(
                scheduleId, 
                1, // Paid
                paidAmount, 
                DateTime.UtcNow, 
                companyBankAccountId, 
                isCash, 
                slipUrl, 
                txId, 
                cancellationToken);

            // Check if all schedules are paid, if so, mark Investment as Repaid (Status = 1)
            if (updated)
            {
                var allSchedules = await investmentRepo.GetSchedulesByInvestmentAsync(investment.InvestmentId, cancellationToken);
                if (allSchedules.All(s => s.Status == 1))
                {
                    investment.Status = 1; // Repaid
                    await investmentRepo.UpdateAsync(investment, cancellationToken);
                }
            }

            return updated;
        }
    }
}
