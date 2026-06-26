using PCS_API.DTOs;

namespace PCS_API.Services;

public interface IFinancialReportService
{
    Task<IEnumerable<TrialBalanceRowDto>> GetTrialBalanceAsync(DateOnly? dateFrom, DateOnly? dateTo, CancellationToken cancellationToken = default);
    Task<IEnumerable<GeneralJournalRowDto>> GetGeneralJournalAsync(DateOnly? dateFrom, DateOnly? dateTo, CancellationToken cancellationToken = default);
    Task<IEnumerable<GeneralLedgerRowDto>> GetGeneralLedgerAsync(int accountId, DateOnly? dateFrom, DateOnly? dateTo, CancellationToken cancellationToken = default);
    Task<ProfitAndLossReportDto> GetProfitAndLossAsync(DateOnly? dateFrom, DateOnly? dateTo, CancellationToken cancellationToken = default);
    Task<BalanceSheetReportDto> GetBalanceSheetAsync(DateOnly asOfDate, CancellationToken cancellationToken = default);
    Task<bool> IsAttachmentUsedAsync(string attachmentUrl, CancellationToken cancellationToken = default);
}
