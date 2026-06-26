using Dapper;
using System.Data;
using PCS_API.Models;

namespace PCS_API.Repositories;

public class AccountTransactionRepository(ISqlConnectionFactory connectionFactory) : IAccountTransactionRepository
{
    public async Task<int> CreateAsync(AccountTransactionModel model, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        var conn = transaction?.Connection ?? connectionFactory.CreateConnection();
        try
        {
            string sql = @"
                INSERT INTO dbo.AccountTransactions (TransactionDate, Type, Amount, ReferenceType, ReferenceId, Notes, CreatedBy, CreatedAt)
                VALUES (@TransactionDate, @Type, @Amount, @ReferenceType, @ReferenceId, @Notes, @CreatedBy, GETDATE());
                SELECT CAST(SCOPE_IDENTITY() as int);";

            var command = new CommandDefinition(sql, model, transaction: transaction, cancellationToken: cancellationToken);
            return await conn.QuerySingleAsync<int>(command);
        }
        finally
        {
            if (transaction == null) 
                conn.Dispose();
        }
    }

    public async Task<int?> GetIdByReferenceAsync(string referenceType, int referenceId, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        var conn = transaction?.Connection ?? connectionFactory.CreateConnection();
        try
        {
            string sql = "SELECT Id FROM dbo.AccountTransactions WHERE ReferenceType = @ReferenceType AND ReferenceId = @ReferenceId";
            var command = new CommandDefinition(sql, new { ReferenceType = referenceType, ReferenceId = referenceId }, transaction: transaction, cancellationToken: cancellationToken);
            return await conn.QuerySingleOrDefaultAsync<int?>(command);
        }
        finally
        {
            if (transaction == null) 
                conn.Dispose();
        }
    }

    public async Task UpdateAsync(int id, decimal amount, string notes, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        var conn = transaction?.Connection ?? connectionFactory.CreateConnection();
        try
        {
            string sql = "UPDATE dbo.AccountTransactions SET Amount = @Amount, Notes = @Notes WHERE Id = @Id";
            var command = new CommandDefinition(sql, new { Id = id, Amount = amount, Notes = notes }, transaction: transaction, cancellationToken: cancellationToken);
            await conn.ExecuteAsync(command);
        }
        finally
        {
            if (transaction == null) 
                conn.Dispose();
        }
    }

    public async Task DeleteAsync(int id, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        var conn = transaction?.Connection ?? connectionFactory.CreateConnection();
        try
        {
            string sql = "DELETE FROM dbo.AccountTransactions WHERE Id = @Id";
            var command = new CommandDefinition(sql, new { Id = id }, transaction: transaction, cancellationToken: cancellationToken);
            await conn.ExecuteAsync(command);
        }
        finally
        {
            if (transaction == null) 
                conn.Dispose();
        }
    }
}
