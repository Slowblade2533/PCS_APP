using Dapper;
using PCS_API.DTOs;

namespace PCS_API.Repositories;

public class ChartOfAccountRepository(ISqlConnectionFactory connectionFactory) : IChartOfAccountRepository
{
    public async Task<IEnumerable<ChartOfAccountDto>> GetAllAccountsAsync(CancellationToken cancellationToken = default)
    {
        using var conn = connectionFactory.CreateConnection();
        const string sql = @"
            SELECT AccountId, AccountCode, AccountName, AccountType, NormalBalance,
                   IsSystemAccount, Description, SortOrder, IsActive
            FROM dbo.ChartOfAccounts
            ORDER BY SortOrder, AccountCode;";
        return await conn.QueryAsync<ChartOfAccountDto>(new CommandDefinition(sql, cancellationToken: cancellationToken));
    }

    public async Task<ChartOfAccountDto?> GetAccountByIdAsync(int accountId, CancellationToken cancellationToken = default)
    {
        using var conn = connectionFactory.CreateConnection();
        const string sql = @"
            SELECT AccountId, AccountCode, AccountName, AccountType, NormalBalance,
                   IsSystemAccount, Description, SortOrder, IsActive
            FROM dbo.ChartOfAccounts WHERE AccountId = @accountId;";
        return await conn.QueryFirstOrDefaultAsync<ChartOfAccountDto>(new CommandDefinition(sql, new { accountId }, cancellationToken: cancellationToken));
    }

    public async Task<int> CheckDuplicateAccountCodeAsync(string accountCode, CancellationToken cancellationToken = default)
    {
        using var conn = connectionFactory.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(
            new CommandDefinition("SELECT COUNT(1) FROM dbo.ChartOfAccounts WHERE AccountCode = @accountCode;", new { accountCode }, cancellationToken: cancellationToken));
    }

    public async Task<int> InsertAccountAsync(ChartOfAccountCreateDto dto, CancellationToken cancellationToken = default)
    {
        using var conn = connectionFactory.CreateConnection();
        const string sql = @"
            INSERT INTO dbo.ChartOfAccounts
                (AccountCode, AccountName, AccountType, NormalBalance, Description, SortOrder)
            OUTPUT INSERTED.AccountId
            VALUES (@AccountCode, @AccountName, @AccountType, @NormalBalance, @Description, @SortOrder);";
        return await conn.QuerySingleAsync<int>(new CommandDefinition(sql, dto, cancellationToken: cancellationToken));
    }
}
