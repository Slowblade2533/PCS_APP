namespace PCS_API.Models;

public class InvestorBankAccountModel
{
    public Guid BankAccountId { get; set; }
    public Guid InvestorId { get; set; }
    public string BankName { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty; // e.g., Savings, Checking
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; }
}
