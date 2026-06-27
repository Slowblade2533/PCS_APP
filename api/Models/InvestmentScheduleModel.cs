namespace PCS_API.Models;

public class InvestmentScheduleModel
{
    public Guid ScheduleId { get; set; }
    public Guid InvestmentId { get; set; }
    public int InstallmentNumber { get; set; }
    public DateTime DueDate { get; set; }
    public decimal PrincipalAmount { get; set; }
    public decimal InterestAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public int Status { get; set; } // 0 = Pending, 1 = Paid, 2 = Overdue
    public DateTime? PaymentDate { get; set; }
    public int? CompanyBankAccountId { get; set; }
    public bool IsCash { get; set; }
    public string? SlipUrl { get; set; }
    public int? TransactionId { get; set; }
    public DateTime CreatedAt { get; set; }
}
