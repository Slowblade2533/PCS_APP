using PCS_API.DTOs;

namespace PCS_API.Repositories;

public interface IFinancialRepository
{
    // Chart of Accounts
    Task<IEnumerable<ChartOfAccountDto>> GetAllAccountsAsync();
    Task<ChartOfAccountDto?> GetAccountByIdAsync(int accountId);
    Task<ResultDto<int>> CreateAccountAsync(ChartOfAccountCreateDto dto);

    // Tax Invoices
    Task<PagedResultDto<TaxInvoiceDto>> GetTaxInvoicesPagedAsync(TaxInvoiceSearchDto search, CancellationToken cancellationToken = default);
    Task<TaxInvoiceDto?> GetTaxInvoiceByIdAsync(int taxInvoiceId);
    Task<ResultDto<int>> CreateTaxInvoiceAsync(TaxInvoiceCreateDto dto);

    // Financial Transactions
    Task<PagedResultDto<FinancialTransactionDto>> GetTransactionsPagedAsync(FinancialTransactionSearchDto search, CancellationToken cancellationToken = default);
    Task<FinancialTransactionDto?> GetTransactionByIdAsync(int transactionId);
    Task<ResultDto<int>> CreateTransactionWithLedgerAsync(FinancialTransactionCreateDto dto);

    // Reports
    Task<IEnumerable<TrialBalanceRowDto>> GetTrialBalanceAsync(DateOnly? dateFrom, DateOnly? dateTo);

    // Helper for cleanup
    Task<bool> IsAttachmentUsedAsync(string attachmentUrl);
}
