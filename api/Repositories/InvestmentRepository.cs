using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using PCS_API.Models;
using PCS_API.Repositories;

namespace PCS_API.Repositories;

public class InvestmentRepository(ISqlConnectionFactory connectionFactory) : IInvestmentRepository
{
    public async Task<IEnumerable<InvestmentModel>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using var conn = connectionFactory.CreateConnection();
        var query = "SELECT * FROM Investment";
        var command = new CommandDefinition(query, cancellationToken: cancellationToken);
        return await conn.QueryAsync<InvestmentModel>(command);
    }

    public async Task<InvestmentModel?> GetByIdAsync(Guid investmentId, CancellationToken cancellationToken = default)
    {
        using var conn = connectionFactory.CreateConnection();
        var query = "SELECT * FROM Investment WHERE InvestmentId = @InvestmentId";
        var command = new CommandDefinition(query, new { InvestmentId = investmentId }, cancellationToken: cancellationToken);
        return await conn.QuerySingleOrDefaultAsync<InvestmentModel>(command);
    }

    public async Task<Guid> CreateAsync(InvestmentModel model, CancellationToken cancellationToken = default)
    {
        using var conn = connectionFactory.CreateConnection();
        var query = @"INSERT INTO Investment (InvestmentId, InvestorId, InvestmentType, PrincipalAmount, Currency, InterestRate, StartDate, MaturityDate, Status, ContractUrl, CompanyBankAccountId, IsCash, CreatedAt, UpdatedAt)
                      VALUES (@InvestmentId, @InvestorId, @InvestmentType, @PrincipalAmount, @Currency, @InterestRate, @StartDate, @MaturityDate, @Status, @ContractUrl, @CompanyBankAccountId, @IsCash, @CreatedAt, @UpdatedAt);
                      SELECT @InvestmentId;";
        var parameters = new
        {
            InvestmentId = model.InvestmentId == Guid.Empty ? Guid.NewGuid() : model.InvestmentId,
            model.InvestorId,
            model.InvestmentType,
            model.PrincipalAmount,
            model.Currency,
            model.InterestRate,
            model.StartDate,
            model.MaturityDate,
            model.Status,
            model.ContractUrl,
            model.CompanyBankAccountId,
            model.IsCash,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var command = new CommandDefinition(query, parameters, cancellationToken: cancellationToken);
        var id = await conn.ExecuteScalarAsync<Guid>(command);
        return id;
    }

    public async Task<bool> UpdateAsync(InvestmentModel model, CancellationToken cancellationToken = default)
    {
        using var conn = connectionFactory.CreateConnection();
        var query = @"UPDATE Investment SET InvestorId = @InvestorId, InvestmentType = @InvestmentType, PrincipalAmount = @PrincipalAmount,
                      Currency = @Currency, InterestRate = @InterestRate, StartDate = @StartDate, MaturityDate = @MaturityDate,
                      Status = @Status, ContractUrl = @ContractUrl, CompanyBankAccountId = @CompanyBankAccountId, IsCash = @IsCash, UpdatedAt = @UpdatedAt WHERE InvestmentId = @InvestmentId";
        var parameters = new
        {
            model.InvestmentId,
            model.InvestorId,
            model.InvestmentType,
            model.PrincipalAmount,
            model.Currency,
            model.InterestRate,
            model.StartDate,
            model.MaturityDate,
            model.Status,
            model.ContractUrl,
            model.CompanyBankAccountId,
            model.IsCash,
            UpdatedAt = DateTime.UtcNow
        };
        var command = new CommandDefinition(query, parameters, cancellationToken: cancellationToken);
        var rows = await conn.ExecuteAsync(command);
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(Guid investmentId, CancellationToken cancellationToken = default)
    {
        using var conn = connectionFactory.CreateConnection();
        var query = "DELETE FROM Investment WHERE InvestmentId = @InvestmentId";
        var command = new CommandDefinition(query, new { InvestmentId = investmentId }, cancellationToken: cancellationToken);
        var rows = await conn.ExecuteAsync(command);
        return rows > 0;
    }

    // Schedule (Installment) methods
    public async Task<IEnumerable<InvestmentScheduleModel>> GetSchedulesByInvestmentAsync(Guid investmentId, CancellationToken cancellationToken = default)
    {
        using var conn = connectionFactory.CreateConnection();
        var query = "SELECT * FROM InvestmentSchedule WHERE InvestmentId = @InvestmentId ORDER BY InstallmentNumber ASC";
        var command = new CommandDefinition(query, new { InvestmentId = investmentId }, cancellationToken: cancellationToken);
        return await conn.QueryAsync<InvestmentScheduleModel>(command);
    }

    public async Task<InvestmentScheduleModel?> GetScheduleByIdAsync(Guid scheduleId, CancellationToken cancellationToken = default)
    {
        using var conn = connectionFactory.CreateConnection();
        var query = "SELECT * FROM InvestmentSchedule WHERE ScheduleId = @ScheduleId";
        var command = new CommandDefinition(query, new { ScheduleId = scheduleId }, cancellationToken: cancellationToken);
        return await conn.QuerySingleOrDefaultAsync<InvestmentScheduleModel>(command);
    }

    public async Task<bool> CreateSchedulesAsync(IEnumerable<InvestmentScheduleModel> schedules, CancellationToken cancellationToken = default)
    {
        using var conn = connectionFactory.CreateConnection();
        var query = @"INSERT INTO InvestmentSchedule (ScheduleId, InvestmentId, InstallmentNumber, DueDate, PrincipalAmount, InterestAmount, PaidAmount, Status, PaymentDate, CompanyBankAccountId, IsCash, SlipUrl, TransactionId, CreatedAt)
                      VALUES (@ScheduleId, @InvestmentId, @InstallmentNumber, @DueDate, @PrincipalAmount, @InterestAmount, @PaidAmount, @Status, @PaymentDate, @CompanyBankAccountId, @IsCash, @SlipUrl, @TransactionId, @CreatedAt)";
        var list = new List<object>();
        foreach (var s in schedules)
        {
            list.Add(new
            {
                ScheduleId = s.ScheduleId == Guid.Empty ? Guid.NewGuid() : s.ScheduleId,
                s.InvestmentId,
                s.InstallmentNumber,
                s.DueDate,
                s.PrincipalAmount,
                s.InterestAmount,
                s.PaidAmount,
                s.Status,
                s.PaymentDate,
                s.CompanyBankAccountId,
                s.IsCash,
                s.SlipUrl,
                s.TransactionId,
                CreatedAt = DateTime.UtcNow
            });
        }
        var rows = await conn.ExecuteAsync(query, list);
        return rows > 0;
    }

    public async Task<bool> UpdateScheduleStatusAsync(Guid scheduleId, int status, decimal paidAmount, DateTime? paymentDate, int? companyBankAccountId, bool isCash, string? slipUrl, int? transactionId, CancellationToken cancellationToken = default)
    {
        using var conn = connectionFactory.CreateConnection();
        var query = @"UPDATE InvestmentSchedule 
                      SET Status = @Status, PaidAmount = @PaidAmount, PaymentDate = @PaymentDate, 
                          CompanyBankAccountId = @CompanyBankAccountId, IsCash = @IsCash, SlipUrl = @SlipUrl, TransactionId = @TransactionId
                      WHERE ScheduleId = @ScheduleId";
        var parameters = new { ScheduleId = scheduleId, Status = status, PaidAmount = paidAmount, PaymentDate = paymentDate, CompanyBankAccountId = companyBankAccountId, IsCash = isCash, SlipUrl = slipUrl, TransactionId = transactionId };
        var command = new CommandDefinition(query, parameters, cancellationToken: cancellationToken);
        var rows = await conn.ExecuteAsync(command);
        return rows > 0;
    }

    // Interest schedule methods
    public async Task<IEnumerable<InvestmentInterestScheduleModel>> GetInterestSchedulesByInvestmentAsync(Guid investmentId, CancellationToken cancellationToken = default)
    {
        using var conn = connectionFactory.CreateConnection();
        var query = "SELECT * FROM InvestmentInterestSchedule WHERE InvestmentId = @InvestmentId ORDER BY StartMonth ASC";
        var command = new CommandDefinition(query, new { InvestmentId = investmentId }, cancellationToken: cancellationToken);
        return await conn.QueryAsync<InvestmentInterestScheduleModel>(command);
    }

    public async Task<bool> CreateInterestSchedulesAsync(IEnumerable<InvestmentInterestScheduleModel> schedules, CancellationToken cancellationToken = default)
    {
        using var conn = connectionFactory.CreateConnection();
        var query = @"INSERT INTO InvestmentInterestSchedule (ScheduleId, InvestmentId, StartMonth, EndMonth, InterestRate, CreatedAt)
                      VALUES (@ScheduleId, @InvestmentId, @StartMonth, @EndMonth, @InterestRate, @CreatedAt)";
        var list = new List<object>();
        foreach (var s in schedules)
        {
            list.Add(new
            {
                ScheduleId = s.ScheduleId == Guid.Empty ? Guid.NewGuid() : s.ScheduleId,
                s.InvestmentId,
                s.StartMonth,
                s.EndMonth,
                s.InterestRate,
                CreatedAt = DateTime.UtcNow
            });
        }
        var rows = await conn.ExecuteAsync(query, list);
        return rows > 0;
    }
}
