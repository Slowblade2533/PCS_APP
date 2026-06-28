using PCS_API.DTOs;
using System.Data;

namespace PCS_API.Repositories;

public interface IFinancialTransactionRepository
{
    Task<FinancialTransactionPagedResultDto> GetTransactionsPagedAsync(FinancialTransactionSearchDto search, CancellationToken cancellationToken = default);
    Task<FinancialTransactionDto?> GetTransactionByIdAsync(int transactionId, CancellationToken cancellationToken = default);
    
    Task<int> GetDailyTransactionCountAsync(DateOnly date, IDbTransaction tx, CancellationToken cancellationToken = default);
    Task<int> InsertTransactionAsync(FinancialTransactionCreateDto dto, IDbTransaction tx, CancellationToken cancellationToken = default);
    Task InsertLedgerEntriesAsync(int transactionId, List<LedgerEntryCreateDto> entries, IDbTransaction tx, CancellationToken cancellationToken = default);

    Task<int> CheckTransactionExistsAsync(int transactionId, IDbTransaction tx, CancellationToken cancellationToken = default);
    Task UpdateTransactionAsync(int transactionId, FinancialTransactionCreateDto dto, IDbTransaction tx, CancellationToken cancellationToken = default);
    Task DeleteLedgerEntriesAsync(int transactionId, IDbTransaction tx, CancellationToken cancellationToken = default);
}
