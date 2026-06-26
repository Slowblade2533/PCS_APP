using PCS_API.DTOs;
using PCS_API.Repositories;
using System.Data;

namespace PCS_API.Services;

public class FinancialTransactionService(IFinancialTransactionRepository transactionRepo, ISqlConnectionFactory connectionFactory) : IFinancialTransactionService
{
    public async Task<PagedResultDto<FinancialTransactionDto>> GetTransactionsPagedAsync(FinancialTransactionSearchDto search, CancellationToken cancellationToken = default)
    {
        return await transactionRepo.GetTransactionsPagedAsync(search, cancellationToken);
    }

    public async Task<FinancialTransactionDto?> GetTransactionByIdAsync(int transactionId, CancellationToken cancellationToken = default)
    {
        return await transactionRepo.GetTransactionByIdAsync(transactionId, cancellationToken);
    }

    public async Task<ResultDto<int>> CreateTransactionWithLedgerAsync(FinancialTransactionCreateDto dto, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        var totalDebit = dto.LedgerEntries.Sum(e => e.DebitAmount);
        var totalCredit = dto.LedgerEntries.Sum(e => e.CreditAmount);
        if (Math.Abs(totalDebit - totalCredit) > 0.01m)
            return ResultDto<int>.Failure($"ยอด Debit ({totalDebit:N2}) ต้องเท่ากับ Credit ({totalCredit:N2}) เสมอ");

        var conn = transaction?.Connection ?? connectionFactory.CreateConnection();
        var tx = transaction;
        bool ownsTransaction = false;

        try
        {
            if (tx == null)
            {
                if (conn.State != ConnectionState.Open) await ((System.Data.Common.DbConnection)conn).OpenAsync(cancellationToken);
                tx = await ((System.Data.Common.DbConnection)conn).BeginTransactionAsync(cancellationToken);
                ownsTransaction = true;
            }

            if (string.IsNullOrWhiteSpace(dto.DocumentNo))
            {
                int count = await transactionRepo.GetDailyTransactionCountAsync(dto.TransactionDate, tx, cancellationToken);
                dto.DocumentNo = $"TX-{dto.TransactionDate:yyyyMMdd}-{(count + 1):D4}";
            }

            if (string.IsNullOrWhiteSpace(dto.Status)) dto.Status = "POSTED";
            if (dto.Status == "POSTED")
            {
                dto.PostedAt ??= DateTime.UtcNow;
                dto.PostedBy ??= dto.CreatedBy;
            }
            else if (dto.Status == "VOID")
            {
                dto.VoidedAt ??= DateTime.UtcNow;
                dto.VoidedBy ??= dto.CreatedBy;
            }

            int txId = await transactionRepo.InsertTransactionAsync(dto, tx, cancellationToken);
            await transactionRepo.InsertLedgerEntriesAsync(txId, dto.LedgerEntries, tx, cancellationToken);

            if (ownsTransaction) await ((System.Data.Common.DbTransaction)tx).CommitAsync(cancellationToken);
            return ResultDto<int>.Success(txId);
        }
        catch
        {
            if (ownsTransaction && tx != null) await ((System.Data.Common.DbTransaction)tx).RollbackAsync(cancellationToken);
            throw;
        }
        finally
        {
            if (ownsTransaction)
            {
                tx?.Dispose();
                conn.Dispose();
            }
        }
    }

    public async Task<ResultDto<int>> UpdateTransactionWithLedgerAsync(int transactionId, FinancialTransactionCreateDto dto, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        var totalDebit = dto.LedgerEntries.Sum(e => e.DebitAmount);
        var totalCredit = dto.LedgerEntries.Sum(e => e.CreditAmount);
        if (Math.Abs(totalDebit - totalCredit) > 0.01m)
            return ResultDto<int>.Failure($"ยอด Debit ({totalDebit:N2}) ต้องเท่ากับ Credit ({totalCredit:N2}) เสมอ");

        var conn = transaction?.Connection ?? connectionFactory.CreateConnection();
        var tx = transaction;
        bool ownsTransaction = false;

        try
        {
            if (tx == null)
            {
                if (conn.State != ConnectionState.Open) await ((System.Data.Common.DbConnection)conn).OpenAsync(cancellationToken);
                tx = await ((System.Data.Common.DbConnection)conn).BeginTransactionAsync(cancellationToken);
                ownsTransaction = true;
            }

            int exists = await transactionRepo.CheckTransactionExistsAsync(transactionId, tx, cancellationToken);
            if (exists == 0) return ResultDto<int>.Failure("ไม่พบข้อมูลรายการบัญชี");

            if (string.IsNullOrWhiteSpace(dto.Status)) dto.Status = "POSTED";

            await transactionRepo.UpdateTransactionAsync(transactionId, dto, tx, cancellationToken);
            await transactionRepo.DeleteLedgerEntriesAsync(transactionId, tx, cancellationToken);
            await transactionRepo.InsertLedgerEntriesAsync(transactionId, dto.LedgerEntries, tx, cancellationToken);

            if (ownsTransaction) await ((System.Data.Common.DbTransaction)tx).CommitAsync(cancellationToken);
            return ResultDto<int>.Success(transactionId);
        }
        catch
        {
            if (ownsTransaction && tx != null) await ((System.Data.Common.DbTransaction)tx).RollbackAsync(cancellationToken);
            throw;
        }
        finally
        {
            if (ownsTransaction)
            {
                tx?.Dispose();
                conn.Dispose();
            }
        }
    }

    public async Task<int?> GetTransactionIdByReferenceAsync(string referenceType, int referenceId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        var conn = transaction?.Connection ?? connectionFactory.CreateConnection();
        try
        {
            string sql = "SELECT TOP 1 TransactionId FROM dbo.FinancialTransactions WHERE ReferenceType = @ReferenceType AND ReferenceId = @ReferenceId AND Status != 'VOID'";
            return await Dapper.SqlMapper.QueryFirstOrDefaultAsync<int?>(conn, new Dapper.CommandDefinition(sql, new { ReferenceType = referenceType, ReferenceId = referenceId }, transaction: transaction, cancellationToken: cancellationToken));
        }
        finally
        {
            if (transaction == null) conn.Dispose();
        }
    }

    public async Task<ResultDto<bool>> DeleteTransactionWithLedgerAsync(int transactionId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        var conn = transaction?.Connection ?? connectionFactory.CreateConnection();
        var tx = transaction;
        bool ownsTransaction = false;

        try
        {
            if (tx == null)
            {
                if (conn.State != ConnectionState.Open) await ((System.Data.Common.DbConnection)conn).OpenAsync(cancellationToken);
                tx = await ((System.Data.Common.DbConnection)conn).BeginTransactionAsync(cancellationToken);
                ownsTransaction = true;
            }

            await transactionRepo.DeleteLedgerEntriesAsync(transactionId, tx, cancellationToken);
            string sql = "DELETE FROM dbo.FinancialTransactions WHERE TransactionId = @Id";
            await Dapper.SqlMapper.ExecuteAsync(conn, new Dapper.CommandDefinition(sql, new { Id = transactionId }, transaction: tx, cancellationToken: cancellationToken));

            if (ownsTransaction) await ((System.Data.Common.DbTransaction)tx).CommitAsync(cancellationToken);
            return ResultDto<bool>.Success(true);
        }
        catch
        {
            if (ownsTransaction && tx != null) await ((System.Data.Common.DbTransaction)tx).RollbackAsync(cancellationToken);
            throw;
        }
        finally
        {
            if (ownsTransaction)
            {
                tx?.Dispose();
                conn.Dispose();
            }
        }
    }
}
