using Dapper;
using PCS_API.Models;

namespace PCS_API.Repositories;

public class PartnerBankAccountRepository(ISqlConnectionFactory connectionFactory) : IPartnerBankAccountRepository
{
    public async Task<IEnumerable<PartnerBankAccountModel>> GetByPartnerAsync(string partnerName, int? supplierId, CancellationToken cancellationToken = default)
    {
        using var conn = connectionFactory.CreateConnection();
        string sql = @"
            SELECT Id, SupplierId, PartnerName, BankName, AccountNo, AccountName, IsDefault, IsActive, CreatedAt FROM dbo.PartnerBankAccounts 
            WHERE IsActive = 1 AND (PartnerName = @PartnerName OR (SupplierId IS NOT NULL AND SupplierId = @SupplierId))
            ORDER BY IsDefault DESC, Id DESC;";
        
        var command = new CommandDefinition(sql, new { PartnerName = partnerName, SupplierId = supplierId }, cancellationToken: cancellationToken);
        return await conn.QueryAsync<PartnerBankAccountModel>(command);
    }

    public async Task<PartnerBankAccountModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        using var conn = connectionFactory.CreateConnection();
        var command = new CommandDefinition(
            "SELECT Id, SupplierId, PartnerName, BankName, AccountNo, AccountName, IsDefault, IsActive, CreatedAt FROM dbo.PartnerBankAccounts WHERE Id = @Id",
            new { Id = id },
            cancellationToken: cancellationToken);
        return await conn.QueryFirstOrDefaultAsync<PartnerBankAccountModel>(command);
    }

    public async Task<int> CreateAsync(PartnerBankAccountModel model, CancellationToken cancellationToken = default)
    {
        using var conn = connectionFactory.CreateConnection();
        
        // If this is set to default, reset others for this partner
        if (model.IsDefault)
        {
            string resetSql = @"
                UPDATE dbo.PartnerBankAccounts 
                SET IsDefault = 0 
                WHERE PartnerName = @PartnerName OR (SupplierId IS NOT NULL AND SupplierId = @SupplierId);";
            await conn.ExecuteAsync(resetSql, new { PartnerName = model.PartnerName, SupplierId = model.SupplierId });
        }

        string sql = @"
            INSERT INTO dbo.PartnerBankAccounts (SupplierId, PartnerName, BankName, AccountNo, AccountName, IsDefault, IsActive, CreatedAt)
            VALUES (@SupplierId, @PartnerName, @BankName, @AccountNo, @AccountName, @IsDefault, @IsActive, GETDATE());
            SELECT CAST(SCOPE_IDENTITY() as int);";

        var command = new CommandDefinition(sql, model, cancellationToken: cancellationToken);
        return await conn.QuerySingleAsync<int>(command);
    }

    public async Task<bool> UpdateAsync(PartnerBankAccountModel model, CancellationToken cancellationToken = default)
    {
        using var conn = connectionFactory.CreateConnection();

        if (model.IsDefault)
        {
            string resetSql = @"
                UPDATE dbo.PartnerBankAccounts 
                SET IsDefault = 0 
                WHERE (PartnerName = @PartnerName OR (SupplierId IS NOT NULL AND SupplierId = @SupplierId)) AND Id != @Id;";
            await conn.ExecuteAsync(resetSql, new { PartnerName = model.PartnerName, SupplierId = model.SupplierId, Id = model.Id });
        }

        string sql = @"
            UPDATE dbo.PartnerBankAccounts
            SET SupplierId = @SupplierId,
                PartnerName = @PartnerName,
                BankName = @BankName,
                AccountNo = @AccountNo,
                AccountName = @AccountName,
                IsDefault = @IsDefault,
                IsActive = @IsActive
            WHERE Id = @Id;";

        var command = new CommandDefinition(sql, model, cancellationToken: cancellationToken);
        int rows = await conn.ExecuteAsync(command);
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        using var conn = connectionFactory.CreateConnection();
        var command = new CommandDefinition(
            "DELETE FROM dbo.PartnerBankAccounts WHERE Id = @Id",
            new { Id = id },
            cancellationToken: cancellationToken);
        int rows = await conn.ExecuteAsync(command);
        return rows > 0;
    }

    public async Task<bool> ExistsAsync(string partnerName, string accountNo, CancellationToken cancellationToken = default)
    {
        using var conn = connectionFactory.CreateConnection();
        string sql = "SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.PartnerBankAccounts WHERE PartnerName = @PartnerName AND AccountNo = @AccountNo) THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END;";
        var command = new CommandDefinition(sql, new { PartnerName = partnerName, AccountNo = accountNo }, cancellationToken: cancellationToken);
        return await conn.QuerySingleAsync<bool>(command);
    }
}

