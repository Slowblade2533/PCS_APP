using System.ComponentModel.DataAnnotations;

namespace PCS_API.DTOs;

// ─── Chart of Accounts ────────────────────────────────────────────────────────
public class ChartOfAccountDto
{
    public int AccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty;
    public string NormalBalance { get; set; } = string.Empty;
    public bool IsSystemAccount { get; set; }
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
}

public class ChartOfAccountCreateDto
{
    [Required, MaxLength(20)]
    public string AccountCode { get; set; } = string.Empty;

    [Required, MaxLength(255)]
    public string AccountName { get; set; } = string.Empty;

    [Required]
    public string AccountType { get; set; } = string.Empty;

    [Required]
    public string NormalBalance { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public int SortOrder { get; set; } = 0;
}

// ─── Tax Invoice ──────────────────────────────────────────────────────────────
public class TaxInvoiceDto
{
    public int TaxInvoiceId { get; set; }
    public string TaxInvoiceNo { get; set; } = string.Empty;
    public DateOnly TaxInvoiceDate { get; set; }
    public string TaxType { get; set; } = string.Empty;
    public string? PartnerName { get; set; }
    public string? PartnerTaxId { get; set; }
    public string? PartnerAddress { get; set; }
    public decimal BaseAmount { get; set; }
    public decimal VatRate { get; set; }
    public decimal VatAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string? DocumentUrl { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class TaxInvoiceCreateDto
{
    [Required, MaxLength(100)]
    public string TaxInvoiceNo { get; set; } = string.Empty;

    [Required]
    public DateOnly TaxInvoiceDate { get; set; }

    [Required]
    public string TaxType { get; set; } = string.Empty;   // INPUT or OUTPUT

    [MaxLength(255)]
    public string? PartnerName { get; set; }

    [MaxLength(20)]
    public string? PartnerTaxId { get; set; }

    [MaxLength(500)]
    public string? PartnerAddress { get; set; }

    [Required]
    public decimal BaseAmount { get; set; }

    public decimal VatRate { get; set; } = 7m;
    public decimal VatAmount { get; set; }
    public decimal TotalAmount { get; set; }

    [MaxLength(500)]
    public string? DocumentUrl { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public int? CreatedBy { get; set; }
}

public class TaxInvoiceSearchDto : PaginationParamsDto
{
    public string? SearchTerm { get; set; }
    public string? TaxType { get; set; }
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
}

// ─── Financial Transaction ────────────────────────────────────────────────────
public class FinancialTransactionDto
{
    public int TransactionId { get; set; }
    public DateOnly TransactionDate { get; set; }
    public string TransactionType { get; set; } = string.Empty;
    public string? TransactionTypeLabel { get; set; }
    public string? ReferenceType { get; set; }
    public int? ReferenceId { get; set; }
    public string? Description { get; set; }
    public decimal TotalAmount { get; set; }
    public string? PaymentMethod { get; set; }
    public string? PaymentRefNo { get; set; }
    public string? SourceAccountInfo { get; set; }
    public string? ReceiverAccountName { get; set; }
    public string? AttachmentUrl { get; set; }
    public string? ReceivedByName { get; set; }
    public DateTime CreatedAt { get; set; }

    // New audit and document fields
    public string? Status { get; set; }
    public int? BranchId { get; set; }
    public string? BranchName { get; set; }
    public DateTime? PostedAt { get; set; }
    public int? PostedBy { get; set; }
    public string? PostedByName { get; set; }
    public DateTime? VoidedAt { get; set; }
    public int? VoidedBy { get; set; }
    public string? VoidedByName { get; set; }
    public string? DocumentNo { get; set; }
    public string? PartnerName { get; set; }
    public DateTime? SlipDateTime { get; set; }
    public string? OriginBank { get; set; }
    public string? DestinationBank { get; set; }

    public List<FinancialLedgerEntryDto> LedgerEntries { get; set; } = new();
}

public class FinancialLedgerEntryDto
{
    public long EntryId { get; set; }
    public int AccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public string? Memo { get; set; }
}

public class FinancialTransactionCreateDto
{
    [Required]
    public DateOnly TransactionDate { get; set; }

    [Required, MaxLength(50)]
    public string TransactionType { get; set; } = string.Empty;

    public string? ReferenceType { get; set; }
    public int? ReferenceId { get; set; }
    public int? TaxInvoiceId { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    [Required]
    public decimal TotalAmount { get; set; }

    // Payment details
    [MaxLength(20)]
    public string? PaymentMethod { get; set; }

    [MaxLength(255)]
    public string? SourceAccountInfo { get; set; }

    [MaxLength(100)]
    public string? PaymentRefNo { get; set; }

    public int? ReceiverAccountId { get; set; }

    [MaxLength(500)]
    public string? AttachmentUrl { get; set; }

    public int? ReceivedBy { get; set; }
    public int? CreatedBy { get; set; }

    // New audit and document fields
    [MaxLength(50)]
    public string? Status { get; set; }

    public int? BranchId { get; set; }
    public DateTime? PostedAt { get; set; }
    public int? PostedBy { get; set; }
    public DateTime? VoidedAt { get; set; }
    public int? VoidedBy { get; set; }

    [MaxLength(50)]
    public string? DocumentNo { get; set; }

    [MaxLength(255)]
    public string? PartnerName { get; set; }

    public DateTime? SlipDateTime { get; set; }

    [MaxLength(100)]
    public string? OriginBank { get; set; }

    [MaxLength(100)]
    public string? DestinationBank { get; set; }

    [Required, MinLength(2, ErrorMessage = "ต้องมีรายการบัญชีอย่างน้อย 2 รายการ (Debit + Credit)")]
    public List<LedgerEntryCreateDto> LedgerEntries { get; set; } = new();
}

public class LedgerEntryCreateDto
{
    [Required]
    public int AccountId { get; set; }

    [Range(0, double.MaxValue)]
    public decimal DebitAmount { get; set; } = 0;

    [Range(0, double.MaxValue)]
    public decimal CreditAmount { get; set; } = 0;

    [MaxLength(255)]
    public string? Memo { get; set; }
}

public class FinancialTransactionSearchDto : PaginationParamsDto
{
    public string? SearchTerm { get; set; }
    public string? TransactionType { get; set; }
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
}

// ─── Trial Balance (งบทดลอง) ─────────────────────────────────────────────────
public class TrialBalanceRowDto
{
    public int AccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty;
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public decimal Balance { get; set; }        // TotalDebit - TotalCredit
}
