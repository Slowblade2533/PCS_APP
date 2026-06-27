namespace PCS_API.Models;

public class InvestmentModel
{
    public Guid InvestmentId { get; set; }
    public Guid InvestorId { get; set; }
    public int InvestmentType { get; set; } // 0 = Equity, 1 = Loan
    public decimal PrincipalAmount { get; set; }
    public string Currency { get; set; } = "THB";
    public decimal? InterestRate { get; set; } // for loans, default null (tiered schedule may apply)
    public DateTime StartDate { get; set; }
    public DateTime? MaturityDate { get; set; }
    public int Status { get; set; } // 0 = Active, 1 = Repaid, 2 = Defaulted, 3 = Cancelled
    public string? ContractUrl { get; set; }
    public int? CompanyBankAccountId { get; set; }
    public bool IsCash { get; set; }
    public string? PaymentProofUrl { get; set; }
    public Guid? InvestorBankAccountId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
