using PCS_API.DTOs;

namespace PCS_API.Services;

public interface IFinancialTransactionService
{
    Task<PagedResultDto<FinancialTransactionDto>> GetTransactionsPagedAsync(FinancialTransactionSearchDto search, CancellationToken cancellationToken = default);
    Task<FinancialTransactionDto?> GetTransactionByIdAsync(int transactionId, CancellationToken cancellationToken = default);
    Task<ResultDto<int>> CreateTransactionWithLedgerAsync(FinancialTransactionCreateDto dto, System.Data.IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<ResultDto<int>> UpdateTransactionWithLedgerAsync(int transactionId, FinancialTransactionCreateDto dto, System.Data.IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<int?> GetTransactionIdByReferenceAsync(string referenceType, int referenceId, System.Data.IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<ResultDto<bool>> DeleteTransactionWithLedgerAsync(int transactionId, System.Data.IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
}

