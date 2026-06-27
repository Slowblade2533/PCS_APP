using PCS_API.DTOs;
using PCS_API.Repositories;

namespace PCS_API.Services;

public class FinancialReportService(IFinancialReportRepository reportRepo) : IFinancialReportService
{
    public async Task<IEnumerable<TrialBalanceRowDto>> GetTrialBalanceAsync(DateOnly? dateFrom, DateOnly? dateTo, CancellationToken cancellationToken = default)
    {
        return await reportRepo.GetTrialBalanceAsync(dateFrom, dateTo, cancellationToken);
    }

    public async Task<IEnumerable<GeneralJournalRowDto>> GetGeneralJournalAsync(DateOnly? dateFrom, DateOnly? dateTo, CancellationToken cancellationToken = default)
    {
        return await reportRepo.GetGeneralJournalAsync(dateFrom, dateTo, cancellationToken);
    }

    public async Task<IEnumerable<GeneralLedgerRowDto>> GetGeneralLedgerAsync(int accountId, DateOnly? dateFrom, DateOnly? dateTo, CancellationToken cancellationToken = default)
    {
        decimal bfBalance = 0;
        if (dateFrom.HasValue)
        {
            bfBalance = await reportRepo.GetBroughtForwardBalanceAsync(accountId, dateFrom, cancellationToken);
        }

        var entries = (await reportRepo.GetLedgerEntriesAsync(accountId, dateFrom, dateTo, cancellationToken)).ToList();

        var result = new List<GeneralLedgerRowDto>();
        if (dateFrom.HasValue)
        {
            result.Add(new GeneralLedgerRowDto
            {
                TransactionDate = dateFrom.Value.AddDays(-1),
                Memo = "ยอดยกมา (Brought Forward)",
                DebitAmount = bfBalance > 0 ? bfBalance : 0,
                CreditAmount = bfBalance < 0 ? Math.Abs(bfBalance) : 0,
                RunningBalance = bfBalance,
                IsBroughtForward = true
            });
        }

        decimal currentBalance = bfBalance;
        foreach (var entry in entries)
        {
            currentBalance += (entry.DebitAmount - entry.CreditAmount);
            entry.RunningBalance = currentBalance;
            result.Add(entry);
        }

        return result;
    }

    public async Task<ProfitAndLossReportDto> GetProfitAndLossAsync(DateOnly? dateFrom, DateOnly? dateTo, CancellationToken cancellationToken = default)
    {
        var rows = await reportRepo.GetProfitAndLossBalancesAsync(dateFrom, dateTo, cancellationToken);
        var report = new ProfitAndLossReportDto { DateFrom = dateFrom, DateTo = dateTo };

        foreach (var row in rows)
        {
            var line = new ProfitAndLossLineDto
            {
                AccountId = row.AccountId,
                AccountCode = row.AccountCode,
                AccountName = row.AccountName
            };

            if (row.AccountType == "REVENUE")
            {
                // Normal balance for Revenue is Credit
                line.Balance = row.TotalCredit - row.TotalDebit;
                report.RevenueLines.Add(line);
                report.TotalRevenue += line.Balance;
            }
            else if (row.AccountType == "EXPENSE")
            {
                // Normal balance for Expense is Debit
                line.Balance = row.TotalDebit - row.TotalCredit;
                report.ExpenseLines.Add(line);
                report.TotalExpense += line.Balance;
            }
        }

        report.NetProfit = report.TotalRevenue - report.TotalExpense;
        return report;
    }

    public async Task<BalanceSheetReportDto> GetBalanceSheetAsync(DateOnly asOfDate, CancellationToken cancellationToken = default)
    {
        var rows = await reportRepo.GetBalanceSheetBalancesAsync(asOfDate, cancellationToken);
        var report = new BalanceSheetReportDto { AsOfDate = asOfDate };

        foreach (var row in rows)
        {
            var line = new BalanceSheetLineDto
            {
                AccountId = row.AccountId,
                AccountCode = row.AccountCode,
                AccountName = row.AccountName
            };

            if (row.AccountType == "ASSET")
            {
                // Normal balance for Asset is Debit
                line.Balance = row.TotalDebit - row.TotalCredit;
                report.AssetLines.Add(line);
                report.TotalAssets += line.Balance;
            }
            else if (row.AccountType == "LIABILITY")
            {
                // Normal balance for Liability is Credit
                line.Balance = row.TotalCredit - row.TotalDebit;
                report.LiabilityLines.Add(line);
                report.TotalLiabilities += line.Balance;
            }
            else if (row.AccountType == "EQUITY")
            {
                // Normal balance for Equity is Credit
                line.Balance = row.TotalCredit - row.TotalDebit;
                report.EquityLines.Add(line);
                report.TotalEquity += line.Balance;
            }
        }

        // Calculate Retained Earnings
        var retainedRows = await reportRepo.GetRetainedEarningsBalancesAsync(asOfDate, cancellationToken);
        decimal totalRev = 0;
        decimal totalExp = 0;

        foreach (var r in retainedRows)
        {
            if (r.AccountType == "REVENUE") totalRev += (r.TotalCredit - r.TotalDebit);
            if (r.AccountType == "EXPENSE") totalExp += (r.TotalDebit - r.TotalCredit);
        }

        decimal retainedEarnings = totalRev - totalExp;
        if (retainedEarnings != 0)
        {
            report.EquityLines.Add(new BalanceSheetLineDto
            {
                AccountId = 0,
                AccountCode = "-",
                AccountName = "กำไร(ขาดทุน)สะสม (Retained Earnings)",
                Balance = retainedEarnings
            });
            report.TotalEquity += retainedEarnings;
        }

        report.TotalLiabilitiesAndEquity = report.TotalLiabilities + report.TotalEquity;
        return report;
    }

    public async Task<bool> IsAttachmentUsedAsync(string attachmentUrl, CancellationToken cancellationToken = default)
    {
        return await reportRepo.IsAttachmentUsedAsync(attachmentUrl, cancellationToken);
    }
}
