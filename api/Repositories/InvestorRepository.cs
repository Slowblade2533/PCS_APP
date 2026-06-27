using Dapper;
using PCS_API.Models;

namespace PCS_API.Repositories;

public class InvestorRepository(ISqlConnectionFactory connectionFactory) : IInvestorRepository
{
    public async Task<IEnumerable<InvestorModel>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using var conn = connectionFactory.CreateConnection();
        var query = "SELECT InvestorId, Title, FirstName, LastName, TaxId, Address, Phone, Email, CreatedAt FROM Investor";
        var command = new CommandDefinition(query, cancellationToken: cancellationToken);
        return await conn.QueryAsync<InvestorModel>(command);
    }

    public async Task<InvestorModel?> GetByIdAsync(Guid investorId, CancellationToken cancellationToken = default)
    {
        using var conn = connectionFactory.CreateConnection();
        var query = "SELECT InvestorId, Title, FirstName, LastName, TaxId, Address, Phone, Email, CreatedAt FROM Investor WHERE InvestorId = @InvestorId";
        var command = new CommandDefinition(query, new { InvestorId = investorId }, cancellationToken: cancellationToken);
        return await conn.QuerySingleOrDefaultAsync<InvestorModel>(command);
    }

    public async Task<Guid> CreateAsync(InvestorModel model, CancellationToken cancellationToken = default)
    {
        using var conn = connectionFactory.CreateConnection();
        var query = @"INSERT INTO Investor (InvestorId, Title, FirstName, LastName, TaxId, Address, Phone, Email, CreatedAt)
                      VALUES (@InvestorId, @Title, @FirstName, @LastName, @TaxId, @Address, @Phone, @Email, @CreatedAt);
                      SELECT @InvestorId;";
        var parameters = new
        {
            InvestorId = model.InvestorId == Guid.Empty ? Guid.NewGuid() : model.InvestorId,
            model.Title,
            model.FirstName,
            model.LastName,
            model.TaxId,
            model.Address,
            model.Phone,
            model.Email,
            CreatedAt = DateTime.UtcNow
        };
        var command = new CommandDefinition(query, parameters, cancellationToken: cancellationToken);
        var id = await conn.ExecuteScalarAsync<Guid>(command);
        return id;
    }

    public async Task<bool> UpdateAsync(InvestorModel model, CancellationToken cancellationToken = default)
    {
        using var conn = connectionFactory.CreateConnection();
        var query = @"UPDATE Investor SET Title = @Title, FirstName = @FirstName, LastName = @LastName,
                      TaxId = @TaxId, Address = @Address, Phone = @Phone, Email = @Email
                      WHERE InvestorId = @InvestorId";
        var command = new CommandDefinition(query, model, cancellationToken: cancellationToken);
        var rows = await conn.ExecuteAsync(command);
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(Guid investorId, CancellationToken cancellationToken = default)
    {
        using var conn = connectionFactory.CreateConnection();
        var query = "DELETE FROM Investor WHERE InvestorId = @InvestorId";
        var command = new CommandDefinition(query, new { InvestorId = investorId }, cancellationToken: cancellationToken);
        var rows = await conn.ExecuteAsync(command);
        return rows > 0;
    }
}

