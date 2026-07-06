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
    private string? _transactionTypeLabel;
    public string? TransactionTypeLabel
    {
        get => string.IsNullOrEmpty(_transactionTypeLabel) ? (TransactionType switch
        {
            "INVESTMENT" => "รับเงินลงทุน",
            "SALES" => "รายได้จากการขาย",
            "PURCHASE_GENERAL" => "ซื้อสินค้าทั่วไป",
            "PURCHASE_VCB" => "สั่งซื้อ VCANBUY",
            "FREIGHT_VCB" => "ค่าขนส่ง VCANBUY",
            "FREIGHT_GENERAL" => "ค่าขนส่งทั่วไป",
            "EXPENSE" => "ค่าใช้จ่าย",
            "STOCK_LOSS" => "สินค้าสูญหาย",
            "SCRAP" => "ตัดจำหน่ายทิ้ง",
            "TRANSFER_IN" => "รับโอนเงิน",
            "RECEIPT" => "รับเงินเข้า",
            "GOODS_RECEIPT" => "ใบรับสินค้า",
            _ => TransactionType
        }) : _transactionTypeLabel;
        set => _transactionTypeLabel = value;
    }
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
    public string? SourceAccountNo { get; set; }
    public string? DestinationAccountNo { get; set; }
    public string? SourceAccountName { get; set; }
    public string? DestinationAccountName { get; set; }

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

    [MaxLength(20)]
    [RegularExpression(@"^[0-9]{10,15}$", ErrorMessage = "เลขที่บัญชีต้องประกอบด้วยตัวเลข 10-15 หลักเท่านั้น")]
    public string? SourceAccountNo { get; set; }

    [MaxLength(20)]
    [RegularExpression(@"^[0-9]{10,15}$", ErrorMessage = "เลขที่บัญชีต้องประกอบด้วยตัวเลข 10-15 หลักเท่านั้น")]
    public string? DestinationAccountNo { get; set; }

    [MaxLength(255)]
    public string? SourceAccountName { get; set; }

    [MaxLength(255)]
    public string? DestinationAccountName { get; set; }

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

public class FinancialTransactionPagedResultDto : PagedResultDto<FinancialTransactionDto>
{
    public decimal TotalIncome { get; set; }
    public decimal TotalExpense { get; set; }
    public decimal NetBalance => TotalIncome - TotalExpense;
    public decimal OpeningBalance { get; set; }
    public decimal CurrentBalance => OpeningBalance + NetBalance;
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

// ─── General Journal (สมุดรายวันทั่วไป) ──────────────────────────────────────
public class GeneralJournalRowDto
{
    public long EntryId { get; set; }
    public int TransactionId { get; set; }
    public DateOnly TransactionDate { get; set; }
    public string? DocumentNo { get; set; }
    public string TransactionType { get; set; } = string.Empty;
    private string? _transactionTypeLabel;
    public string? TransactionTypeLabel
    {
        get => string.IsNullOrEmpty(_transactionTypeLabel) ? (TransactionType switch
        {
            "INVESTMENT" => "รับเงินลงทุน",
            "SALES" => "รายได้จากการขาย",
            "PURCHASE_GENERAL" => "ซื้อสินค้าทั่วไป",
            "PURCHASE_VCB" => "สั่งซื้อ VCANBUY",
            "FREIGHT_VCB" => "ค่าขนส่ง VCANBUY",
            "FREIGHT_GENERAL" => "ค่าขนส่งทั่วไป",
            "EXPENSE" => "ค่าใช้จ่าย",
            "STOCK_LOSS" => "สินค้าสูญหาย",
            "SCRAP" => "ตัดจำหน่ายทิ้ง",
            "TRANSFER_IN" => "รับโอนเงิน",
            "RECEIPT" => "รับเงินเข้า",
            "GOODS_RECEIPT" => "ใบรับสินค้า",
            _ => TransactionType
        }) : _transactionTypeLabel;
        set => _transactionTypeLabel = value;
    }
    public string? Memo { get; set; }
    public int AccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }
}

// ─── General Ledger (บัญชีแยกประเภท) ───────────────────────────────────────
public class GeneralLedgerRowDto
{
    public DateOnly? TransactionDate { get; set; }
    public string? DocumentNo { get; set; }
    public string? Memo { get; set; }
    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public decimal RunningBalance { get; set; }
    public bool IsBroughtForward { get; set; }
}

// ─── Profit & Loss (งบกำไรขาดทุน) ─────────────────────────────────────────
public class ProfitAndLossReportDto
{
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
    public List<ProfitAndLossLineDto> RevenueLines { get; set; } = new();
    public decimal TotalRevenue { get; set; }
    public List<ProfitAndLossLineDto> ExpenseLines { get; set; } = new();
    public decimal TotalExpense { get; set; }
    public decimal NetProfit { get; set; }
}

public class ProfitAndLossLineDto
{
    public int AccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public decimal Balance { get; set; } // Positive usually means normal balance
}

// ─── Balance Sheet (งบดุล) ────────────────────────────────────────────────
public class BalanceSheetReportDto
{
    public DateOnly AsOfDate { get; set; }
    
    public List<BalanceSheetLineDto> AssetLines { get; set; } = new();
    public decimal TotalAssets { get; set; }
    
    public List<BalanceSheetLineDto> LiabilityLines { get; set; } = new();
    public decimal TotalLiabilities { get; set; }
    
    public List<BalanceSheetLineDto> EquityLines { get; set; } = new();
    public decimal TotalEquity { get; set; }
    
    public decimal TotalLiabilitiesAndEquity { get; set; }
}

public class BalanceSheetLineDto
{
    public int AccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public decimal Balance { get; set; }
}

// ─── Partner Bank Account DTOs ────────────────────────────────────────────────
public class PartnerBankAccountDto
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

public class PartnerBankAccountCreateDto
{
    public int? SupplierId { get; set; }

    [Required]
    [MaxLength(255)]
    public string PartnerName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string BankName { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    [RegularExpression(@"^[0-9]{10,15}$", ErrorMessage = "เลขที่บัญชีต้องประกอบด้วยตัวเลข 10-15 หลักเท่านั้น")]
    public string AccountNo { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string AccountName { get; set; } = string.Empty;

    public bool IsDefault { get; set; } = false;
    public bool IsActive { get; set; } = true;
}

// ─── Company Bank Account DTOs ────────────────────────────────────────────────
public class CompanyBankAccountDto
{
    public int Id { get; set; }
    public string BankName { get; set; } = string.Empty;
    public string AccountNo { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public int? ChartOfAccountId { get; set; }
    public bool IsActive { get; set; }
    public string AccountType { get; set; } = "Business";
    public DateTime CreatedAt { get; set; }
}

public class CompanyBankAccountCreateDto
{
    [Required]
    [MaxLength(50)]
    public string BankName { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    [RegularExpression(@"^[0-9\-]{10,20}$", ErrorMessage = "เลขที่บัญชีต้องประกอบด้วยตัวเลขหรือขีด 10-20 หลัก")]
    public string AccountNo { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string AccountName { get; set; } = string.Empty;

    public int? ChartOfAccountId { get; set; }
    public bool IsActive { get; set; } = true;

    [Required]
    [MaxLength(50)]
    public string AccountType { get; set; } = "Business";
}
