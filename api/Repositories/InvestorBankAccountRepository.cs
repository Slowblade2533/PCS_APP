using Dapper;
using PCS_API.Models;

namespace PCS_API.Repositories;

public class InvestorBankAccountRepository(ISqlConnectionFactory connectionFactory) : IInvestorBankAccountRepository
{
    public async Task<IEnumerable<InvestorBankAccountModel>> GetByInvestorAsync(Guid investorId, CancellationToken cancellationToken = default)
    {
        using var conn = connectionFactory.CreateConnection();
        var query = "SELECT BankAccountId, InvestorId, BankName, AccountNumber, AccountType, IsDefault, CreatedAt FROM InvestorBankAccount WHERE InvestorId = @InvestorId";
        var command = new CommandDefinition(query, new { InvestorId = investorId }, cancellationToken: cancellationToken);
        return await conn.QueryAsync<InvestorBankAccountModel>(command);
    }

    public async Task<InvestorBankAccountModel?> GetByIdAsync(Guid bankAccountId, CancellationToken cancellationToken = default)
    {
        using var conn = connectionFactory.CreateConnection();
        var query = "SELECT BankAccountId, InvestorId, BankName, AccountNumber, AccountType, IsDefault, CreatedAt FROM InvestorBankAccount WHERE BankAccountId = @BankAccountId";
        var command = new CommandDefinition(query, new { BankAccountId = bankAccountId }, cancellationToken: cancellationToken);
        return await conn.QuerySingleOrDefaultAsync<InvestorBankAccountModel>(command);
    }

    public async Task<Guid> CreateAsync(InvestorBankAccountModel model, CancellationToken cancellationToken = default)
    {
        using var conn = connectionFactory.CreateConnection();
        var query = @"INSERT INTO InvestorBankAccount (BankAccountId, InvestorId, BankName, AccountNumber, AccountType, IsDefault, CreatedAt)
                          VALUES (@BankAccountId, @InvestorId, @BankName, @AccountNumber, @AccountType, @IsDefault, @CreatedAt);
                          SELECT @BankAccountId;";
        var parameters = new
        {
            BankAccountId = model.BankAccountId == Guid.Empty ? Guid.NewGuid() : model.BankAccountId,
            model.InvestorId,
            model.BankName,
            model.AccountNumber,
            model.AccountType,
            model.IsDefault,
            CreatedAt = DateTime.UtcNow
        };
        var command = new CommandDefinition(query, parameters, cancellationToken: cancellationToken);
        var id = await conn.ExecuteScalarAsync<Guid>(command);
        return id;
    }

    public async Task<bool> UpdateAsync(InvestorBankAccountModel model, CancellationToken cancellationToken = default)
    {
        using var conn = connectionFactory.CreateConnection();
        var query = @"UPDATE InvestorBankAccount 
                          SET BankName = @BankName, AccountNumber = @AccountNumber, AccountType = @AccountType, IsDefault = @IsDefault 
                          WHERE BankAccountId = @BankAccountId";
        var command = new CommandDefinition(query, model, cancellationToken: cancellationToken);
        var rows = await conn.ExecuteAsync(command);
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(Guid bankAccountId, CancellationToken cancellationToken = default)
    {
        using var conn = connectionFactory.CreateConnection();
        var query = "DELETE FROM InvestorBankAccount WHERE BankAccountId = @BankAccountId";
        var command = new CommandDefinition(query, new { BankAccountId = bankAccountId }, cancellationToken: cancellationToken);
        var rows = await conn.ExecuteAsync(command);
        return rows > 0;
    }
}