using Dapper;
using PCS_API.Models;

namespace PCS_API.Repositories;

using Microsoft.Extensions.Logging;

public class InvestmentRepository(ISqlConnectionFactory connectionFactory, ILogger<InvestmentRepository> logger) : IInvestmentRepository
{
    public async Task<IEnumerable<InvestmentModel>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using var conn = connectionFactory.CreateConnection();
        var query = "SELECT InvestmentId, InvestorId, InvestmentType, PrincipalAmount, Currency, InterestRate, StartDate, MaturityDate, Status, ContractUrl, CompanyBankAccountId, IsCash, PaymentProofUrl, InvestorBankAccountId, CreatedAt, UpdatedAt FROM Investment";
        var command = new CommandDefinition(query, cancellationToken: cancellationToken);
        return await conn.QueryAsync<InvestmentModel>(command);
    }

    public async Task<InvestmentModel?> GetByIdAsync(Guid investmentId, CancellationToken cancellationToken = default)
    {
        using var conn = connectionFactory.CreateConnection();
        var query = "SELECT InvestmentId, InvestorId, InvestmentType, PrincipalAmount, Currency, InterestRate, StartDate, MaturityDate, Status, ContractUrl, CompanyBankAccountId, IsCash, PaymentProofUrl, InvestorBankAccountId, CreatedAt, UpdatedAt FROM Investment WHERE InvestmentId = @InvestmentId";
        var command = new CommandDefinition(query, new { InvestmentId = investmentId }, cancellationToken: cancellationToken);
        return await conn.QuerySingleOrDefaultAsync<InvestmentModel>(command);
    }

    public async Task<Guid> CreateAsync(InvestmentModel model, CancellationToken cancellationToken = default)
    {
        using var conn = connectionFactory.CreateConnection();
        var query = @"INSERT INTO Investment (InvestmentId, InvestorId, InvestmentType, PrincipalAmount, Currency, InterestRate, StartDate, MaturityDate, Status, ContractUrl, PaymentProofUrl, CompanyBankAccountId, IsCash, InvestorBankAccountId, CreatedAt, UpdatedAt)
                      VALUES (@InvestmentId, @InvestorId, @InvestmentType, @PrincipalAmount, @Currency, @InterestRate, @StartDate, @MaturityDate, @Status, @ContractUrl, @PaymentProofUrl, @CompanyBankAccountId, @IsCash, @InvestorBankAccountId, @CreatedAt, @UpdatedAt);
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
            model.PaymentProofUrl,
            model.CompanyBankAccountId,
            model.IsCash,
            model.InvestorBankAccountId,
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
                      Status = @Status, ContractUrl = @ContractUrl, PaymentProofUrl = @PaymentProofUrl, CompanyBankAccountId = @CompanyBankAccountId, IsCash = @IsCash, InvestorBankAccountId = @InvestorBankAccountId, UpdatedAt = @UpdatedAt WHERE InvestmentId = @InvestmentId";
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
            model.PaymentProofUrl,
            model.CompanyBankAccountId,
            model.IsCash,
            model.InvestorBankAccountId,
            UpdatedAt = DateTime.UtcNow
        };
        var command = new CommandDefinition(query, parameters, cancellationToken: cancellationToken);
        var rows = await conn.ExecuteAsync(command);
        return rows > 0;
    }

    public async Task<bool> UpdateInvestmentAsync(InvestmentModel model, List<InvestmentScheduleModel> schedules, List<InvestmentInterestScheduleModel> interestSchedules, CancellationToken cancellationToken = default)
    {
        using var conn = connectionFactory.CreateConnection();
        conn.Open();
        using var transaction = conn.BeginTransaction();
        try
        {
            // 1. Update Investment
            var queryUpdate = @"UPDATE Investment SET InvestorId = @InvestorId, InvestmentType = @InvestmentType, PrincipalAmount = @PrincipalAmount,
                               Currency = @Currency, InterestRate = @InterestRate, StartDate = @StartDate, MaturityDate = @MaturityDate,
                               Status = @Status, ContractUrl = @ContractUrl, PaymentProofUrl = @PaymentProofUrl, CompanyBankAccountId = @CompanyBankAccountId, IsCash = @IsCash, InvestorBankAccountId = @InvestorBankAccountId, UpdatedAt = @UpdatedAt WHERE InvestmentId = @InvestmentId";
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
                model.PaymentProofUrl,
                model.CompanyBankAccountId,
                model.IsCash,
                model.InvestorBankAccountId,
                UpdatedAt = DateTime.UtcNow
            };
            var cmdUpdate = new CommandDefinition(queryUpdate, parameters, transaction, cancellationToken: cancellationToken);
            await conn.ExecuteAsync(cmdUpdate);

            // 2. Delete existing InterestSchedules and recreate
            var deleteInterestQuery = "DELETE FROM InvestmentInterestSchedule WHERE InvestmentId = @InvestmentId";
            var cmdDeleteInterest = new CommandDefinition(deleteInterestQuery, new { model.InvestmentId }, transaction, cancellationToken: cancellationToken);
            await conn.ExecuteAsync(cmdDeleteInterest);

            if (interestSchedules != null && interestSchedules.Count > 0)
            {
                var insertInterestQuery = @"INSERT INTO InvestmentInterestSchedule (ScheduleId, InvestmentId, StartMonth, EndMonth, InterestRate, CreatedAt)
                                            VALUES (@ScheduleId, @InvestmentId, @StartMonth, @EndMonth, @InterestRate, @CreatedAt)";
                var interestList = interestSchedules.Select(ins => new
                {
                    ScheduleId = ins.ScheduleId == Guid.Empty ? Guid.NewGuid() : ins.ScheduleId,
                    InvestmentId = model.InvestmentId,
                    ins.StartMonth,
                    ins.EndMonth,
                    ins.InterestRate,
                    CreatedAt = DateTime.UtcNow
                }).ToList();
                var cmdInsertInterest = new CommandDefinition(insertInterestQuery, interestList, transaction, cancellationToken: cancellationToken);
                await conn.ExecuteAsync(cmdInsertInterest);
            }

            // 3. Update schedules
            var existingSchedules = (await conn.QueryAsync<InvestmentScheduleModel>(
                new CommandDefinition("SELECT ScheduleId, InvestmentId, InstallmentNumber, DueDate, PrincipalAmount, InterestAmount, PaidAmount, Status, PaymentDate, CompanyBankAccountId, IsCash, SlipUrl, TransactionId, CreatedAt FROM InvestmentSchedule WHERE InvestmentId = @InvestmentId", new { model.InvestmentId }, transaction, cancellationToken: cancellationToken))).ToList();

            bool hasPaidSchedules = existingSchedules.Any(s => s.Status == 1);

            if (!hasPaidSchedules)
            {
                var deleteSchedulesQuery = "DELETE FROM InvestmentSchedule WHERE InvestmentId = @InvestmentId";
                var cmdDeleteSchedules = new CommandDefinition(deleteSchedulesQuery, new { model.InvestmentId }, transaction, cancellationToken: cancellationToken);
                await conn.ExecuteAsync(cmdDeleteSchedules);

                if (schedules != null && schedules.Count > 0)
                {
                    var insertSchedulesQuery = @"INSERT INTO InvestmentSchedule (ScheduleId, InvestmentId, InstallmentNumber, DueDate, PrincipalAmount, InterestAmount, PaidAmount, Status, PaymentDate, CompanyBankAccountId, IsCash, SlipUrl, TransactionId, CreatedAt)
                                                VALUES (@ScheduleId, @InvestmentId, @InstallmentNumber, @DueDate, @PrincipalAmount, @InterestAmount, @PaidAmount, @Status, @PaymentDate, @CompanyBankAccountId, @IsCash, @SlipUrl, @TransactionId, @CreatedAt)";
                    var schedulesList = schedules.Select(s => new
                    {
                        ScheduleId = s.ScheduleId == Guid.Empty ? Guid.NewGuid() : s.ScheduleId,
                        InvestmentId = model.InvestmentId,
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
                    }).ToList();
                    var cmdInsertSchedules = new CommandDefinition(insertSchedulesQuery, schedulesList, transaction, cancellationToken: cancellationToken);
                    await conn.ExecuteAsync(cmdInsertSchedules);
                }
            }

            transaction.Commit();
            return true;
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            logger.LogError(ex, "Transaction failed and rolled back");
            throw;
        }
    }

    public async Task<bool> DeleteAsync(Guid investmentId, CancellationToken cancellationToken = default)
    {
        using var conn = connectionFactory.CreateConnection();
        conn.Open();
        using var transaction = conn.BeginTransaction();
        try
        {
            // 1. Delete InvestmentInterestSchedule records
            var deleteInterestQuery = "DELETE FROM InvestmentInterestSchedule WHERE InvestmentId = @InvestmentId";
            var cmdDeleteInterest = new CommandDefinition(deleteInterestQuery, new { InvestmentId = investmentId }, transaction, cancellationToken: cancellationToken);
            await conn.ExecuteAsync(cmdDeleteInterest);

            // 2. Delete InvestmentSchedule records
            var deleteSchedulesQuery = "DELETE FROM InvestmentSchedule WHERE InvestmentId = @InvestmentId";
            var cmdDeleteSchedules = new CommandDefinition(deleteSchedulesQuery, new { InvestmentId = investmentId }, transaction, cancellationToken: cancellationToken);
            await conn.ExecuteAsync(cmdDeleteSchedules);

            // 3. Find and delete related FinancialTransactions & FinancialLedgerEntries
            var refPattern = $"%Ref: {investmentId}%";
            var txIdsQuery = "SELECT TransactionId FROM dbo.FinancialTransactions WHERE Description LIKE @RefPattern";
            var cmdTxIds = new CommandDefinition(txIdsQuery, new { RefPattern = refPattern }, transaction, cancellationToken: cancellationToken);
            var txIds = (await conn.QueryAsync<int>(cmdTxIds)).ToList();

            if (txIds.Any())
            {
                var deleteLedgerQuery = "DELETE FROM dbo.FinancialLedgerEntries WHERE TransactionId IN @TxIds";
                var cmdDeleteLedger = new CommandDefinition(deleteLedgerQuery, new { TxIds = txIds }, transaction, cancellationToken: cancellationToken);
                await conn.ExecuteAsync(cmdDeleteLedger);

                var deleteTxQuery = "DELETE FROM dbo.FinancialTransactions WHERE TransactionId IN @TxIds";
                var cmdDeleteTx = new CommandDefinition(deleteTxQuery, new { TxIds = txIds }, transaction, cancellationToken: cancellationToken);
                await conn.ExecuteAsync(cmdDeleteTx);
            }

            // 4. Delete the main Investment record
            var deleteInvestmentQuery = "DELETE FROM Investment WHERE InvestmentId = @InvestmentId";
            var cmdDeleteInvestment = new CommandDefinition(deleteInvestmentQuery, new { InvestmentId = investmentId }, transaction, cancellationToken: cancellationToken);
            var rows = await conn.ExecuteAsync(cmdDeleteInvestment);

            transaction.Commit();
            return rows > 0;
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            logger.LogError(ex, "Transaction failed and rolled back");
            throw;
        }
    }

    // Schedule (Installment) methods
    public async Task<IEnumerable<InvestmentScheduleModel>> GetSchedulesByInvestmentAsync(Guid investmentId, CancellationToken cancellationToken = default)
    {
        using var conn = connectionFactory.CreateConnection();
        var query = "SELECT ScheduleId, InvestmentId, InstallmentNumber, DueDate, PrincipalAmount, InterestAmount, PaidAmount, Status, PaymentDate, CompanyBankAccountId, IsCash, SlipUrl, TransactionId, CreatedAt FROM InvestmentSchedule WHERE InvestmentId = @InvestmentId ORDER BY InstallmentNumber ASC";
        var command = new CommandDefinition(query, new { InvestmentId = investmentId }, cancellationToken: cancellationToken);
        return await conn.QueryAsync<InvestmentScheduleModel>(command);
    }

    public async Task<InvestmentScheduleModel?> GetScheduleByIdAsync(Guid scheduleId, CancellationToken cancellationToken = default)
    {
        using var conn = connectionFactory.CreateConnection();
        var query = "SELECT ScheduleId, InvestmentId, InstallmentNumber, DueDate, PrincipalAmount, InterestAmount, PaidAmount, Status, PaymentDate, CompanyBankAccountId, IsCash, SlipUrl, TransactionId, CreatedAt FROM InvestmentSchedule WHERE ScheduleId = @ScheduleId";
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
        var query = "SELECT ScheduleId, InvestmentId, StartMonth, EndMonth, InterestRate, CreatedAt FROM InvestmentInterestSchedule WHERE InvestmentId = @InvestmentId ORDER BY StartMonth ASC";
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


