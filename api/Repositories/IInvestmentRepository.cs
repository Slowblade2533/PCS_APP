using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using PCS_API.Models;
using PCS_API.Repositories;

namespace PCS_API.Repositories;

public interface IInvestmentRepository
{
    Task<IEnumerable<InvestmentModel>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<InvestmentModel?> GetByIdAsync(Guid investmentId, CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(InvestmentModel model, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(InvestmentModel model, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid investmentId, CancellationToken cancellationToken = default);

    // Schedule (Installment) methods
    Task<IEnumerable<InvestmentScheduleModel>> GetSchedulesByInvestmentAsync(Guid investmentId, CancellationToken cancellationToken = default);
    Task<InvestmentScheduleModel?> GetScheduleByIdAsync(Guid scheduleId, CancellationToken cancellationToken = default);
    Task<bool> CreateSchedulesAsync(IEnumerable<InvestmentScheduleModel> schedules, CancellationToken cancellationToken = default);
    Task<bool> UpdateScheduleStatusAsync(Guid scheduleId, int status, decimal paidAmount, DateTime? paymentDate, int? companyBankAccountId, bool isCash, string? slipUrl, int? transactionId, CancellationToken cancellationToken = default);

    // Interest schedule methods
    Task<IEnumerable<InvestmentInterestScheduleModel>> GetInterestSchedulesByInvestmentAsync(Guid investmentId, CancellationToken cancellationToken = default);
    Task<bool> CreateInterestSchedulesAsync(IEnumerable<InvestmentInterestScheduleModel> schedules, CancellationToken cancellationToken = default);
}
