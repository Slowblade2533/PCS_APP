using System;

namespace PCS_API.DTOs
{
    public class InvestorBankAccountDto
    {
        public Guid BankAccountId { get; set; }
        public Guid InvestorId { get; set; }
        public string BankName { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string AccountType { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
