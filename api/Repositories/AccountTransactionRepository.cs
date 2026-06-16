using Dapper;
using PCS_API.Models;

namespace PCS_API.Repositories;

public class AccountTransactionRepository : IAccountTransactionRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public AccountTransactionRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<int> CreateAsync(AccountTransactionModel model, CancellationToken cancellationToken = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        string sql = @"
            INSERT INTO dbo.AccountTransactions (TransactionDate, Type, Amount, ReferenceType, ReferenceId, Notes, CreatedBy, CreatedAt)
            VALUES (@TransactionDate, @Type, @Amount, @ReferenceType, @ReferenceId, @Notes, @CreatedBy, GETDATE());
            SELECT CAST(SCOPE_IDENTITY() as int);";

        var command = new CommandDefinition(sql, model, cancellationToken: cancellationToken);
        return await conn.QuerySingleAsync<int>(command);
    }
}
