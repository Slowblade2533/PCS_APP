using System;
using System.Collections.Generic;

namespace PCS_API.DTOs
{
    public class InvestmentDto
    {
        public Guid InvestmentId { get; set; }
        public Guid InvestorId { get; set; }
        public int InvestmentType { get; set; } // 0 = Equity, 1 = Loan
        public decimal PrincipalAmount { get; set; }
        public string Currency { get; set; } = "THB";
        public decimal? InterestRate { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? MaturityDate { get; set; }
        public int Status { get; set; } // 0 = Active, 1 = Repaid, 2 = Defaulted, 3 = Cancelled
        public string? ContractUrl { get; set; }
        public string? PaymentProofUrl { get; set; }
        public Guid? InvestorBankAccountId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public List<InvestmentScheduleDto> Schedules { get; set; } = new();
        public List<InvestmentInterestScheduleDto> InterestSchedules { get; set; } = new();
    }

    public class InvestmentScheduleDto
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

    public class InvestmentInterestScheduleDto
    {
        public Guid ScheduleId { get; set; }
        public Guid InvestmentId { get; set; }
        public int StartMonth { get; set; }
        public int EndMonth { get; set; }
        public decimal InterestRate { get; set; }
    }

    public class InvestmentCreateDto
    {
        public Guid InvestorId { get; set; }
        public int InvestmentType { get; set; } // 0 = Equity, 1 = Loan
        public decimal PrincipalAmount { get; set; }
        public string Currency { get; set; } = "THB";
        public decimal? InterestRate { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? MaturityDate { get; set; }
        public string? ContractUrl { get; set; }
        public string? PaymentProofUrl { get; set; }
        public int? CompanyBankAccountId { get; set; }
        public bool IsCash { get; set; }
        public Guid? InvestorBankAccountId { get; set; }

        public List<InvestmentInterestScheduleCreateDto> InterestSchedules { get; set; } = new();
        public List<InvestmentScheduleCreateDto> Schedules { get; set; } = new();
    }

    public class InvestmentInterestScheduleCreateDto
    {
        public int StartMonth { get; set; }
        public int EndMonth { get; set; }
        public decimal InterestRate { get; set; }
    }

    public class InvestmentScheduleCreateDto
    {
        public int InstallmentNumber { get; set; }
        public DateTime DueDate { get; set; }
        public decimal PrincipalAmount { get; set; }
        public decimal InterestAmount { get; set; }
    }

    public class InvestmentRepaymentDto
    {
        public decimal PaidAmount { get; set; }
        public int? CompanyBankAccountId { get; set; }
        public bool IsCash { get; set; }
        public string? SlipUrl { get; set; }
    }
}
