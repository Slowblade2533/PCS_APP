using PCS_API.DTOs;
using PCS_API.Models;
using PCS_API.Repositories;
using Dapper;

namespace PCS_API.Services;

public class InvestmentService(
    IInvestmentRepository investmentRepo,
    IInvestorRepository investorRepo,
    IFinancialTransactionService transactionService,
    ICompanyBankAccountRepository companyBankAccountRepo,
    IInvestorBankAccountRepository investorBankAccountRepo,
    ISqlConnectionFactory connectionFactory,
    IWebHostEnvironment env,
    ILogger<InvestmentService> logger) : IInvestmentService
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

        var webRoot = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
        model.ContractUrl = AttachmentHelper.CommitAttachment(model.ContractUrl, webRoot, "transactions");
        model.PaymentProofUrl = AttachmentHelper.CommitAttachment(model.PaymentProofUrl, webRoot, "transactions");

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
            string? destinationBank = null;
            string? destinationAccountNo = null;
            string? destinationAccountName = null;

            if (!model.IsCash && model.CompanyBankAccountId.HasValue)
            {
                var bankAcc = await companyBankAccountRepo.GetByIdAsync(model.CompanyBankAccountId.Value, cancellationToken);
                if (bankAcc != null)
                {
                    destinationBank = bankAcc.BankName;
                    destinationAccountNo = bankAcc.AccountNo;
                    destinationAccountName = bankAcc.AccountName;

                    if (bankAcc.ChartOfAccountId.HasValue)
                    {
                        targetCashBankAccountId = bankAcc.ChartOfAccountId.Value;
                    }
                    else
                    {
                        targetCashBankAccountId = 2; // Default Bank (เงินฝากธนาคาร)
                    }
                }
            }

            int creditAccountId = model.InvestmentType == 0 ? 9 : 20; // 9 = ทุนเจ้าของ (3000), 20 = เงินกู้ยืมจากผู้ลงทุน (2100)

            string? originBank = null;
            string? sourceAccountNo = null;
            string? sourceAccountName = null;

            if (model.InvestorBankAccountId.HasValue)
            {
                var sourceBankAcc = await investorBankAccountRepo.GetByIdAsync(model.InvestorBankAccountId.Value, cancellationToken);
                if (sourceBankAcc != null)
                {
                    originBank = sourceBankAcc.BankName;
                    sourceAccountNo = sourceBankAcc.AccountNumber;
                    sourceAccountName = $"{investor.FirstName} {investor.LastName}";
                }
            }

            var journalDto = new FinancialTransactionCreateDto
            {
                TransactionDate = DateOnly.FromDateTime(model.StartDate),
                TransactionType = "INVESTMENT",
                Description = $"รับเงินร่วมลงทุน / เงินกู้ยืม จากคุณ {investor.FirstName} {investor.LastName} [Ref: {investmentId}]",
                TotalAmount = model.PrincipalAmount,
                PaymentMethod = model.IsCash ? "CASH" : "TRANSFER",
                AttachmentUrl = model.PaymentProofUrl ?? model.ContractUrl,
                Status = "POSTED",
                OriginBank = originBank,
                SourceAccountNo = sourceAccountNo,
                SourceAccountName = sourceAccountName,
                DestinationBank = destinationBank,
                DestinationAccountNo = destinationAccountNo,
                DestinationAccountName = destinationAccountName,
                LedgerEntries = new List<LedgerEntryCreateDto>
                {
                    new() { AccountId = targetCashBankAccountId, DebitAmount = model.PrincipalAmount, CreditAmount = 0, Memo = "รับเงินทุนเข้า" },
                    new() { AccountId = creditAccountId, DebitAmount = 0, CreditAmount = model.PrincipalAmount, Memo = "บันทึกบัญชีเจ้าหนี้/ทุนผู้ลงทุน" }
                }
            };

            await transactionService.CreateTransactionWithLedgerAsync(journalDto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create accounting journal entry for new investment {InvestmentId}.", investmentId);
            throw;
        }

        return investmentId;
    }

    public async Task<bool> UpdateAsync(InvestmentModel model, CancellationToken cancellationToken = default)
    {
        return await investmentRepo.UpdateAsync(model, cancellationToken);
    }

    public async Task<bool> UpdateAsync(InvestmentModel model, List<InvestmentScheduleModel> schedules, List<InvestmentInterestScheduleModel> interestSchedules, CancellationToken cancellationToken = default)
    {
        // Verify investor exists
        var investor = await investorRepo.GetByIdAsync(model.InvestorId, cancellationToken);
        if (investor == null)
        {
            throw new ArgumentException("Investor not found.");
        }

        var webRoot = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
        model.ContractUrl = AttachmentHelper.CommitAttachment(model.ContractUrl, webRoot, "transactions");
        model.PaymentProofUrl = AttachmentHelper.CommitAttachment(model.PaymentProofUrl, webRoot, "transactions");

        var success = await investmentRepo.UpdateInvestmentAsync(model, schedules, interestSchedules, cancellationToken);
        if (!success) return false;

        // Auto-update accounting journal entry for the capital receipt
        try
        {
            using var conn = connectionFactory.CreateConnection();
            var txId = await conn.ExecuteScalarAsync<int?>(
                "SELECT TransactionId FROM dbo.FinancialTransactions WHERE TransactionType = 'INVESTMENT' AND Description LIKE @Ref",
                new { Ref = $"%Ref: {model.InvestmentId}%" });

            if (txId.HasValue)
            {
                int targetCashBankAccountId = 1; // Default Cash (เงินสด)
                string? destinationBank = null;
                string? destinationAccountNo = null;
                string? destinationAccountName = null;

                if (!model.IsCash && model.CompanyBankAccountId.HasValue)
                {
                    var bankAcc = await companyBankAccountRepo.GetByIdAsync(model.CompanyBankAccountId.Value, cancellationToken);
                    if (bankAcc != null)
                    {
                        destinationBank = bankAcc.BankName;
                        destinationAccountNo = bankAcc.AccountNo;
                        destinationAccountName = bankAcc.AccountName;

                        if (bankAcc.ChartOfAccountId.HasValue)
                        {
                            targetCashBankAccountId = bankAcc.ChartOfAccountId.Value;
                        }
                        else
                        {
                            targetCashBankAccountId = 2; // Default Bank (เงินฝากธนาคาร)
                        }
                    }
                }

                int creditAccountId = model.InvestmentType == 0 ? 9 : 20; // 9 = ทุนเจ้าของ (3000), 20 = เงินกู้ยืมจากผู้ลงทุน (2100)

                string? originBank = null;
                string? sourceAccountNo = null;
                string? sourceAccountName = null;

                if (model.InvestorBankAccountId.HasValue)
                {
                    var sourceBankAcc = await investorBankAccountRepo.GetByIdAsync(model.InvestorBankAccountId.Value, cancellationToken);
                    if (sourceBankAcc != null)
                    {
                        originBank = sourceBankAcc.BankName;
                        sourceAccountNo = sourceBankAcc.AccountNumber;
                        sourceAccountName = $"{investor.FirstName} {investor.LastName}";
                    }
                }

                var journalDto = new FinancialTransactionCreateDto
                {
                    TransactionDate = DateOnly.FromDateTime(model.StartDate),
                    TransactionType = "INVESTMENT",
                    Description = $"รับเงินร่วมลงทุน / เงินกู้ยืม จากคุณ {investor.FirstName} {investor.LastName} [Ref: {model.InvestmentId}]",
                    TotalAmount = model.PrincipalAmount,
                    PaymentMethod = model.IsCash ? "CASH" : "TRANSFER",
                    AttachmentUrl = model.PaymentProofUrl ?? model.ContractUrl,
                    Status = "POSTED",
                    OriginBank = originBank,
                    SourceAccountNo = sourceAccountNo,
                    SourceAccountName = sourceAccountName,
                    DestinationBank = destinationBank,
                    DestinationAccountNo = destinationAccountNo,
                    DestinationAccountName = destinationAccountName,
                    LedgerEntries = new List<LedgerEntryCreateDto>
                    {
                        new() { AccountId = targetCashBankAccountId, DebitAmount = model.PrincipalAmount, CreditAmount = 0, Memo = "รับเงินทุนเข้า" },
                        new() { AccountId = creditAccountId, DebitAmount = 0, CreditAmount = model.PrincipalAmount, Memo = "บันทึกบัญชีเจ้าหนี้/ทุนผู้ลงทุน" }
                    }
                };

                await transactionService.UpdateTransactionWithLedgerAsync(txId.Value, journalDto);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update accounting journal entry for investment {InvestmentId}.", model.InvestmentId);
            throw;
        }

        return true;
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
        var webRoot = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
        slipUrl = AttachmentHelper.CommitAttachment(slipUrl, webRoot, "transactions");

        var schedule = await investmentRepo.GetScheduleByIdAsync(scheduleId, cancellationToken);
        if (schedule == null || schedule.Status == 1) return false;

        var investment = await investmentRepo.GetByIdAsync(schedule.InvestmentId, cancellationToken);
        if (investment == null) return false;

        var investor = await investorRepo.GetByIdAsync(investment.InvestorId, cancellationToken);
        string investorName = investor != null ? $"{investor.FirstName} {investor.LastName}" : "ผู้ลงทุน";

        int targetCashBankAccountId = 1; // Default Cash (เงินสด)
        string? originBank = null;
        string? sourceAccountNo = null;
        string? sourceAccountName = null;

        if (!isCash && companyBankAccountId.HasValue)
        {
            var bankAcc = await companyBankAccountRepo.GetByIdAsync(companyBankAccountId.Value, cancellationToken);
            if (bankAcc != null)
            {
                originBank = bankAcc.BankName;
                sourceAccountNo = bankAcc.AccountNo;
                sourceAccountName = bankAcc.AccountName;

                if (bankAcc.ChartOfAccountId.HasValue)
                {
                    targetCashBankAccountId = bankAcc.ChartOfAccountId.Value;
                }
                else
                {
                    targetCashBankAccountId = 2; // Default Bank (เงินฝากธนาคาร)
                }
            }
        }

        string? destinationBank = null;
        string? destinationAccountNo = null;
        string? destinationAccountName = null;

        if (!isCash && investment.InvestorBankAccountId.HasValue)
        {
            var destBankAcc = await investorBankAccountRepo.GetByIdAsync(investment.InvestorBankAccountId.Value, cancellationToken);
            if (destBankAcc != null)
            {
                destinationBank = destBankAcc.BankName;
                destinationAccountNo = destBankAcc.AccountNumber;
                destinationAccountName = investorName;
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
                OriginBank = originBank,
                SourceAccountNo = sourceAccountNo,
                SourceAccountName = sourceAccountName,
                DestinationBank = destinationBank,
                DestinationAccountNo = destinationAccountNo,
                DestinationAccountName = destinationAccountName,
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

            var journalResult = await transactionService.CreateTransactionWithLedgerAsync(journalDto);
            if (journalResult.IsSuccess)
            {
                txId = journalResult.Value;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create accounting journal entry for repayment of schedule {ScheduleId}.", scheduleId);
            throw;
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
