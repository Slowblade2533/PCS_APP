using PCS_API.Models;

namespace PCS_API.Services;

public interface IInvestmentService
{
    Task<IEnumerable<InvestmentModel>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<InvestmentModel?> GetByIdAsync(Guid investmentId, CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(InvestmentModel model, List<InvestmentScheduleModel> schedules, List<InvestmentInterestScheduleModel> interestSchedules, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(InvestmentModel model, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(InvestmentModel model, List<InvestmentScheduleModel> schedules, List<InvestmentInterestScheduleModel> interestSchedules, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid investmentId, CancellationToken cancellationToken = default);
    Task<IEnumerable<InvestmentScheduleModel>> GetSchedulesByInvestmentAsync(Guid investmentId, CancellationToken cancellationToken = default);
    Task<IEnumerable<InvestmentInterestScheduleModel>> GetInterestSchedulesByInvestmentAsync(Guid investmentId, CancellationToken cancellationToken = default);
    Task<bool> RecordRepaymentAsync(Guid scheduleId, decimal paidAmount, int? companyBankAccountId, bool isCash, string? slipUrl, CancellationToken cancellationToken = default);
}
