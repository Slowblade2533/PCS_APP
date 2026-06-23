namespace PCS_API.Models;

// ─── Chart of Accounts ───────────────────────────────────────────────────────
public class ChartOfAccountModel
{
    public int AccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty;   // Asset, Liability, Equity, Revenue, Expense
    public string NormalBalance { get; set; } = string.Empty; // Debit or Credit
    public bool IsSystemAccount { get; set; }
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
}

// ─── Tax Invoice ──────────────────────────────────────────────────────────────
public class TaxInvoiceModel
{
    public int TaxInvoiceId { get; set; }
    public string TaxInvoiceNo { get; set; } = string.Empty;
    public DateOnly TaxInvoiceDate { get; set; }
    public string TaxType { get; set; } = string.Empty;   // INPUT or OUTPUT
    public string? PartnerName { get; set; }
    public string? PartnerTaxId { get; set; }
    public string? PartnerAddress { get; set; }
    public decimal BaseAmount { get; set; }
    public decimal VatRate { get; set; }
    public decimal VatAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string? DocumentUrl { get; set; }
    public string? Notes { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ─── Financial Transaction ────────────────────────────────────────────────────
public class FinancialTransactionModel
{
    public int TransactionId { get; set; }
    public DateOnly TransactionDate { get; set; }
    public string TransactionType { get; set; } = string.Empty;
    public string? ReferenceType { get; set; }
    public int? ReferenceId { get; set; }
    public int? TaxInvoiceId { get; set; }
    public string? Description { get; set; }
    public decimal TotalAmount { get; set; }

    // Payment details
    public string? PaymentMethod { get; set; }       // CASH, TRANSFER, CREDIT
    public string? SourceAccountInfo { get; set; }
    public string? PaymentRefNo { get; set; }
    public int? ReceiverAccountId { get; set; }
    public string? AttachmentUrl { get; set; }       // JPG,PNG,WEBP,GIF,PDF — optional

    public int? ReceivedBy { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // New audit and document fields
    public string? Status { get; set; }
    public int? BranchId { get; set; }
    public DateTime? PostedAt { get; set; }
    public int? PostedBy { get; set; }
    public DateTime? VoidedAt { get; set; }
    public int? VoidedBy { get; set; }
    public string? DocumentNo { get; set; }
    public string? PartnerName { get; set; }
    public DateTime? SlipDateTime { get; set; }
    public string? OriginBank { get; set; }
    public string? DestinationBank { get; set; }
    public string? SourceAccountNo { get; set; }
    public string? DestinationAccountNo { get; set; }
    public string? SourceAccountName { get; set; }
    public string? DestinationAccountName { get; set; }
}

// ─── Financial Ledger Entry (Double-Entry) ────────────────────────────────────
public class FinancialLedgerEntryModel
{
    public long EntryId { get; set; }
    public int TransactionId { get; set; }
    public int AccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public string? Memo { get; set; }
}

// ─── Partner Bank Account ─────────────────────────────────────────────────────
public class PartnerBankAccountModel
{
    public int Id { get; set; }
    public int? SupplierId { get; set; }
    public string PartnerName { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string AccountNo { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ─── Company Bank Account ─────────────────────────────────────────────────────
public class CompanyBankAccountModel
{
    public int Id { get; set; }
    public string BankName { get; set; } = string.Empty;
    public string AccountNo { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public int? ChartOfAccountId { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
