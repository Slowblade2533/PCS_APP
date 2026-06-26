using PCS_API.DTOs;

namespace PCS_API.Repositories;

public interface IFinancialReportRepository
{
    Task<IEnumerable<TrialBalanceRowDto>> GetTrialBalanceAsync(DateOnly? dateFrom, DateOnly? dateTo, CancellationToken cancellationToken = default);
    Task<IEnumerable<GeneralJournalRowDto>> GetGeneralJournalAsync(DateOnly? dateFrom, DateOnly? dateTo, CancellationToken cancellationToken = default);
    
    Task<decimal> GetBroughtForwardBalanceAsync(int accountId, DateOnly? dateFrom, CancellationToken cancellationToken = default);
    Task<IEnumerable<GeneralLedgerRowDto>> GetLedgerEntriesAsync(int accountId, DateOnly? dateFrom, DateOnly? dateTo, CancellationToken cancellationToken = default);

    Task<IEnumerable<ReportAccountBalanceDto>> GetProfitAndLossBalancesAsync(DateOnly? dateFrom, DateOnly? dateTo, CancellationToken cancellationToken = default);
    Task<IEnumerable<ReportAccountBalanceDto>> GetBalanceSheetBalancesAsync(DateOnly asOfDate, CancellationToken cancellationToken = default);
    Task<IEnumerable<ReportAccountBalanceDto>> GetRetainedEarningsBalancesAsync(DateOnly asOfDate, CancellationToken cancellationToken = default);
    
    Task<bool> IsAttachmentUsedAsync(string attachmentUrl, CancellationToken cancellationToken = default);
}
