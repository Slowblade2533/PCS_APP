using Dapper;
using PCS_API.Models;

namespace PCS_API.Repositories;

public class CompanyBankAccountRepository : ICompanyBankAccountRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public CompanyBankAccountRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<CompanyBankAccountModel>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        string sql = @"
            SELECT * FROM dbo.CompanyBankAccounts 
            WHERE IsActive = 1
            ORDER BY Id DESC;";
        var command = new CommandDefinition(sql, cancellationToken: cancellationToken);
        return await conn.QueryAsync<CompanyBankAccountModel>(command);
    }

    public async Task<CompanyBankAccountModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        var command = new CommandDefinition(
            "SELECT * FROM dbo.CompanyBankAccounts WHERE Id = @Id",
            new { Id = id },
            cancellationToken: cancellationToken);
        return await conn.QueryFirstOrDefaultAsync<CompanyBankAccountModel>(command);
    }

    public async Task<int> CreateAsync(CompanyBankAccountModel model, CancellationToken cancellationToken = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        string sql = @"
            INSERT INTO dbo.CompanyBankAccounts (BankName, AccountNo, AccountName, ChartOfAccountId, IsActive, CreatedAt)
            VALUES (@BankName, @AccountNo, @AccountName, @ChartOfAccountId, @IsActive, GETDATE());
            SELECT CAST(SCOPE_IDENTITY() as int);";

        var command = new CommandDefinition(sql, model, cancellationToken: cancellationToken);
        return await conn.QuerySingleAsync<int>(command);
    }

    public async Task<bool> UpdateAsync(CompanyBankAccountModel model, CancellationToken cancellationToken = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        string sql = @"
            UPDATE dbo.CompanyBankAccounts
            SET BankName = @BankName,
                AccountNo = @AccountNo,
                AccountName = @AccountName,
                ChartOfAccountId = @ChartOfAccountId,
                IsActive = @IsActive
            WHERE Id = @Id;";

        var command = new CommandDefinition(sql, model, cancellationToken: cancellationToken);
        int rows = await conn.ExecuteAsync(command);
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        var command = new CommandDefinition(
            "DELETE FROM dbo.CompanyBankAccounts WHERE Id = @Id",
            new { Id = id },
            cancellationToken: cancellationToken);
        int rows = await conn.ExecuteAsync(command);
        return rows > 0;
    }
}
